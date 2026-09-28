using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Selection;
using YelcoBot.CodeSweep.Domain.Services;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Engine
{
    public class RoslynCodeCleaner : ICodeCleaner
    {
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
            if (solution == null)
            {
                stopwatch.Stop();
                summary.Duration = stopwatch.Elapsed;
                return summary;
            }

            List<DocumentId> targetDocumentIds = GetTargetDocumentIds(solution, selection);
            if (!targetDocumentIds.Any())
            {
                stopwatch.Stop();
                summary.Duration = stopwatch.Elapsed;
                return summary;
            }

            Solution currentSolution = solution;
            int total = targetDocumentIds.Count;
            int current = 0;

            foreach (DocumentId docId in targetDocumentIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    summary.IsCancelled = true;
                    break;
                }

                Document? doc = currentSolution.GetDocument(docId);
                if (doc == null || string.IsNullOrWhiteSpace(doc.FilePath))
                {
                    continue;
                }

                current++;
                progress?.Report(new SweepProgress(current, total, doc.FilePath!));

                try
                {
                    if (options.IgnoreGeneratedCode)
                    {
                        Microsoft.CodeAnalysis.Text.SourceText text = await doc.GetTextAsync(cancellationToken);
                        if (_generatedCodeDetector.IsGeneratedCode(doc.FilePath!, text.ToString()))
                        {
                            continue;
                        }
                    }

                    summary.ProcessedFilesCount++;
                    Microsoft.CodeAnalysis.Text.SourceText originalText = await doc.GetTextAsync(cancellationToken);
                    Document cleanedDoc = await _documentSweeper.SweepAsync(doc, options, cancellationToken);
                    Microsoft.CodeAnalysis.Text.SourceText cleanedText = await cleanedDoc.GetTextAsync(cancellationToken);

                    if (!originalText.ContentEquals(cleanedText))
                    {
                        currentSolution = cleanedDoc.Project.Solution;
                        summary.ChangedFilesCount++;
                    }
                }
                catch (Exception ex)
                {
                    summary.Failures.Add(new SweepFailure(doc.FilePath!, ex.Message));
                }
            }

            if (summary.ChangedFilesCount > 0 && !summary.IsCancelled)
            {
                await _workspaceAccessor.TryApplyChangesAsync(currentSolution, cancellationToken);
            }

            stopwatch.Stop();
            summary.Duration = stopwatch.Elapsed;
            return summary;
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
                    IReadOnlyList<DocumentId> openIds = _workspaceAccessor.GetOpenDocumentIds();
                    result.AddRange(openIds);
                    break;

                case DocumentSelectionType.ActiveDocument:
                case DocumentSelectionType.SpecificFile:
                    if (!string.IsNullOrEmpty(selection.FilePath))
                    {
                        IEnumerable<DocumentId> documentIds = solution.GetDocumentIdsWithFilePath(selection.FilePath);
                        result.AddRange(documentIds);
                    }
                    break;
            }

            return result;
        }
    }
}
