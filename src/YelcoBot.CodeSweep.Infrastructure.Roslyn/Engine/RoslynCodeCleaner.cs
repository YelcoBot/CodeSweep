using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Selection;
using YelcoBot.CodeSweep.Domain.Services;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Engine
{
    /// <summary>
    /// Limpia documentos C#/VB en paralelo y en background sobre una foto inmutable de la solución,
    /// y aplica todos los cambios en un único TryApplyChanges.
    /// </summary>
    public class RoslynCodeCleaner : ICodeCleaner
    {
        private const int GeneratedHeaderSampleLength = 1024;

        private readonly IWorkspaceAccessor _workspaceAccessor;
        private readonly DocumentSweeper _documentSweeper;
        private readonly GeneratedCodeDetector _generatedCodeDetector;
        private readonly ILooseFileStore _looseFileStore;

        public RoslynCodeCleaner(
            IWorkspaceAccessor workspaceAccessor,
            DocumentSweeper documentSweeper,
            GeneratedCodeDetector generatedCodeDetector,
            ILooseFileStore looseFileStore)
        {
            _workspaceAccessor = workspaceAccessor;
            _documentSweeper = documentSweeper;
            _generatedCodeDetector = generatedCodeDetector;
            _looseFileStore = looseFileStore;
        }

        public async Task<SweepSummary> CleanAsync(
            DocumentSelection selection,
            SweepOptions options,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            SweepSummary summary = new SweepSummary();

            Solution? solution = _workspaceAccessor.CurrentSolution;
            List<DocumentId> targetDocumentIds = solution == null ? new List<DocumentId>() : GetTargetDocumentIds(solution, selection);

            List<string> looseFilePaths = GetLooseFilePaths(solution, selection);

            if (solution == null || targetDocumentIds.Count == 0)
            {
                await CleanLooseFilesAsync(looseFilePaths, options, summary, progress, cancellationToken).ConfigureAwait(false);
                summary.Duration = stopwatch.Elapsed;
                return summary;
            }

            ConcurrentBag<(DocumentId Id, SourceText Text)> changedDocuments = new();
            ConcurrentBag<SweepFailure> failures = new();
            int total = targetDocumentIds.Count;
            int started = 0;
            int processed = 0;

            using (SemaphoreSlim throttler = new SemaphoreSlim(Environment.ProcessorCount))
            {
                IEnumerable<Task> tasks = targetDocumentIds.Select(documentId => Task.Run(async () =>
                {
                    await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        Document? document = solution.GetDocument(documentId);
                        if (document?.FilePath == null)
                            return;

                        progress?.Report(new SweepProgress(Interlocked.Increment(ref started), total, document.FilePath));

                        try
                        {
                            SourceText originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            if (options.IgnoreGeneratedCode && IsGenerated(document.FilePath, originalText))
                                return;

                            Interlocked.Increment(ref processed);

                            Document cleanedDocument = await _documentSweeper.SweepAsync(document, options, cancellationToken).ConfigureAwait(false);
                            SourceText cleanedText = await cleanedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                            if (!originalText.ContentEquals(cleanedText))
                            {
                                changedDocuments.Add((documentId, cleanedText));
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            failures.Add(new SweepFailure(document.FilePath, ex.Message));
                        }
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }, cancellationToken));

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Se reporta abajo como cancelado.
                }
            }

            summary.ProcessedFilesCount = processed;
            summary.Failures.AddRange(failures);

            if (cancellationToken.IsCancellationRequested)
            {
                summary.IsCancelled = true;
                summary.Duration = stopwatch.Elapsed;
                return summary;
            }

            if (!changedDocuments.IsEmpty)
            {
                Solution newSolution = solution;
                foreach ((DocumentId id, SourceText text) in changedDocuments)
                {
                    newSolution = newSolution.WithDocumentText(id, text);
                }

                if (await _workspaceAccessor.TryApplyChangesAsync(newSolution, cancellationToken))
                {
                    summary.ChangedFilesCount = changedDocuments.Count;
                }
                else
                {
                    summary.Failures.Add(new SweepFailure(
                        "(workspace)",
                        Strings.FailureWorkspaceChanged));
                }
            }

            await CleanLooseFilesAsync(looseFilePaths, options, summary, progress, cancellationToken).ConfigureAwait(false);

            summary.Duration = stopwatch.Elapsed;
            return summary;
        }

        /// <summary>
        /// Returns the C#/VB files requested by path that belong to no loaded project
        /// (a folder opened without a solution).
        /// </summary>
        private static List<string> GetLooseFilePaths(Solution? solution, DocumentSelection selection)
        {
            if (selection.Type != DocumentSelectionType.Files && selection.Type != DocumentSelectionType.ActiveDocument)
                return new List<string>();

            return selection.FilePaths
                .Where(p => GetLanguage(p) != null)
                .Where(p => solution == null || solution.GetDocumentIdsWithFilePath(p).IsEmpty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? GetLanguage(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                return LanguageNames.CSharp;

            if (extension.Equals(".vb", StringComparison.OrdinalIgnoreCase))
                return LanguageNames.VisualBasic;

            return null;
        }

        /// <summary>
        /// Cleans files that belong to no project. Without a project there are no references or compilation,
        /// so only the syntax and formatting rules run.
        /// </summary>
        private async Task CleanLooseFilesAsync(
            List<string> filePaths,
            SweepOptions options,
            SweepSummary summary,
            IProgress<SweepProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (filePaths.Count == 0)
                return;

            SweepOptions syntaxOnlyOptions = options.WithoutSemanticRules();

            using (AdhocWorkspace workspace = new AdhocWorkspace())
            {
                for (int i = 0; i < filePaths.Count && !summary.IsCancelled; i++)
                {
                    string filePath = filePaths[i];
                    progress?.Report(new SweepProgress(i + 1, filePaths.Count, filePath));

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await CleanLooseFileAsync(workspace, filePath, syntaxOnlyOptions, summary, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        summary.IsCancelled = true;
                    }
                    catch (Exception ex)
                    {
                        summary.Failures.Add(new SweepFailure(filePath, ex.Message));
                    }
                }
            }
        }

        private async Task CleanLooseFileAsync(
            AdhocWorkspace workspace,
            string filePath,
            SweepOptions options,
            SweepSummary summary,
            CancellationToken cancellationToken)
        {
            ILooseFile? file = await _looseFileStore.OpenAsync(filePath, cancellationToken).ConfigureAwait(false);
            if (file == null)
                return;

            SourceText originalText = SourceText.From(file.Text);
            if (options.IgnoreGeneratedCode && IsGenerated(filePath, originalText))
                return;

            summary.ProcessedFilesCount++;

            Document document = GetLooseProject(workspace, GetLanguage(filePath)!).AddDocument(Path.GetFileName(filePath), originalText, filePath: filePath);
            Document cleanedDocument = await _documentSweeper.SweepAsync(document, options, cancellationToken).ConfigureAwait(false);
            string cleanedText = (await cleanedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();

            if (cleanedText != file.Text && await file.ApplyAsync(cleanedText, cancellationToken).ConfigureAwait(false))
            {
                summary.ChangedFilesCount++;
                summary.ChangedFilePaths.Add(filePath);
            }
        }

        private static Project GetLooseProject(AdhocWorkspace workspace, string language)
        {
            return workspace.CurrentSolution.Projects.FirstOrDefault(p => p.Language == language)
                ?? workspace.AddProject("CodeSweep.LooseFiles." + language, language);
        }

        private bool IsGenerated(string filePath, SourceText text)
        {
            string header = text.ToString(new TextSpan(0, Math.Min(GeneratedHeaderSampleLength, text.Length)));
            return _generatedCodeDetector.IsGeneratedCode(filePath, header);
        }

        private List<DocumentId> GetTargetDocumentIds(Solution solution, DocumentSelection selection)
        {
            List<DocumentId> result = new List<DocumentId>();

            switch (selection.Type)
            {
                case DocumentSelectionType.Solution:
                    foreach (Project project in solution.Projects)
                    {
                        result.AddRange(project.Documents.Select(d => d.Id));
                    }
                    break;

                case DocumentSelectionType.OpenDocuments:
                    result.AddRange(_workspaceAccessor.GetOpenDocumentIds());
                    break;

                case DocumentSelectionType.ActiveDocument:
                case DocumentSelectionType.Files:
                    foreach (string filePath in selection.FilePaths)
                    {
                        // Un archivo enlazado (multi-target, shared project) tiene varios DocumentId: basta con uno.
                        DocumentId? documentId = solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
                        if (documentId != null)
                        {
                            result.Add(documentId);
                        }
                    }
                    break;
            }

            // Mismo archivo en varios proyectos → procesarlo una sola vez.
            return result
                .GroupBy(id => solution.GetDocument(id)?.FilePath ?? id.Id.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }
    }
}
