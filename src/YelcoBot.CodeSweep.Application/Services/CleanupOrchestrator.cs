using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Routing;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.Services
{
    public class CleanupOrchestrator
    {
        private readonly IDocumentProvider _documentProvider;
        private readonly DocumentRouter _documentRouter;
        private readonly ICodeCleaner _codeCleaner;
        private readonly IEditorFormatter _editorFormatter;
        private readonly IWebFormsDesignerGenerator _webFormsDesignerGenerator;
        private readonly SweepActivity _activity;

        public CleanupOrchestrator(
            IDocumentProvider documentProvider,
            DocumentRouter documentRouter,
            ICodeCleaner codeCleaner,
            IEditorFormatter editorFormatter,
            IWebFormsDesignerGenerator webFormsDesignerGenerator,
            SweepActivity activity)
        {
            _documentProvider = documentProvider;
            _documentRouter = documentRouter;
            _codeCleaner = codeCleaner;
            _editorFormatter = editorFormatter;
            _webFormsDesignerGenerator = webFormsDesignerGenerator;
            _activity = activity;
        }

        public async Task<SweepSummary> ExecuteAsync(
            DocumentSelection selection,
            SweepOptions options,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            using (_activity.Begin())
            {
                return await ExecuteCoreAsync(selection, options, progress, cancellationToken);
            }
        }

        private async Task<SweepSummary> ExecuteCoreAsync(
            DocumentSelection selection,
            SweepOptions options,
            IProgress<SweepProgress>? progress,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<string> filePaths = await _documentProvider.GetFilePathsAsync(selection, cancellationToken);

            List<string> distinctPaths = filePaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<string> roslynFiles = distinctPaths.Where(p => _documentRouter.Route(p, options) == CleanupEngine.Roslyn).ToList();
            List<string> editorFiles = distinctPaths.Where(p => _documentRouter.Route(p, options) == CleanupEngine.Editor).ToList();

            SweepSummary summary = new SweepSummary();

            // Fase 1: Roslyn, en background y en paralelo.
            if (roslynFiles.Count > 0)
            {
                SweepSummary roslynSummary = await _codeCleaner.CleanAsync(DocumentSelection.Files(roslynFiles), options, progress, cancellationToken);
                summary.Merge(roslynSummary);
            }

            if (summary.IsCancelled || cancellationToken.IsCancellationRequested)
            {
                summary.IsCancelled = true;
                return summary;
            }

            // Fase 2: editor de VS, secuencial en el hilo de UI.
            if (editorFiles.Count > 0)
            {
                SweepSummary editorSummary = await _editorFormatter.FormatAsync(editorFiles, progress, cancellationToken);
                summary.Merge(editorSummary);
            }

            // Fase 3 (opcional): regenerar los .designer.cs de los Web Forms que cambiaron, una sola vez al final.
            if (options.RegenerateWebFormsDesigner && !summary.IsCancelled && !cancellationToken.IsCancellationRequested)
            {
                List<string> changedWebForms = summary.ChangedFilePaths
                    .Where(p => FileTypeGroups.WebFormsWithDesigner.Contains(Path.GetExtension(p)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (changedWebForms.Count > 0)
                {
                    summary.Failures.AddRange(await _webFormsDesignerGenerator.RegenerateAsync(changedWebForms, cancellationToken));
                }
            }

            return summary;
        }
    }
}
