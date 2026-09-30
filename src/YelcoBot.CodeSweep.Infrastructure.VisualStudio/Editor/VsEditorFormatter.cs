using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// Formatea archivos que Roslyn no soporta (aspx, razor, html, xml…) con el editor de VS.
    /// Secuencial en el hilo de UI; para cada archivo prueba las estrategias en orden.
    /// </summary>
    public class VsEditorFormatter : IEditorFormatter
    {
        private readonly IReadOnlyList<IEditorFormatStrategy> _strategies;

        public VsEditorFormatter(IEnumerable<IEditorFormatStrategy> strategies)
        {
            _strategies = strategies.OrderBy(s => s.Order).ToList();
        }

        public async Task<SweepSummary> FormatAsync(
            IReadOnlyList<string> filePaths,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            SweepSummary summary = new SweepSummary();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            for (int i = 0; i < filePaths.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    summary.IsCancelled = true;
                    break;
                }

                string filePath = filePaths[i];
                progress?.Report(new SweepProgress(i + 1, filePaths.Count, filePath));

                try
                {
                    EditorFormatOutcome outcome = await FormatFileAsync(filePath, cancellationToken);
                    if (outcome != EditorFormatOutcome.NotHandled)
                    {
                        summary.ProcessedFilesCount++;
                    }

                    if (outcome == EditorFormatOutcome.Changed)
                    {
                        summary.ChangedFilesCount++;
                        summary.ChangedFilePaths.Add(filePath);
                    }
                }
                catch (Exception ex)
                {
                    summary.Failures.Add(new SweepFailure(filePath, ex.Message));
                }

                // Ceder el hilo de UI entre archivos para que VS siga respondiendo.
                await Task.Yield();
            }

            stopwatch.Stop();
            summary.Duration = stopwatch.Elapsed;
            return summary;
        }

        private async Task<EditorFormatOutcome> FormatFileAsync(string filePath, CancellationToken cancellationToken)
        {
            foreach (IEditorFormatStrategy strategy in _strategies)
            {
                EditorFormatOutcome outcome = await strategy.TryFormatAsync(filePath, cancellationToken);
                if (outcome != EditorFormatOutcome.NotHandled)
                    return outcome;
            }

            return EditorFormatOutcome.NotHandled;
        }
    }
}
