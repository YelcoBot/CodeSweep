using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.Application.Abstractions;
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

        public RoslynCodeCleaner(
            IWorkspaceAccessor workspaceAccessor,
            DocumentSweeper documentSweeper,
            GeneratedCodeDetector generatedCodeDetector)
        {
            _workspaceAccessor = workspaceAccessor;
            _documentSweeper = documentSweeper;
            _generatedCodeDetector = generatedCodeDetector;
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

            if (solution == null || targetDocumentIds.Count == 0)
            {
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

            summary.Duration = stopwatch.Elapsed;
            return summary;
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
