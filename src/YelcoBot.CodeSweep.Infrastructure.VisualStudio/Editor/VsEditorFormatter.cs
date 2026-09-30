using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// Formatea archivos que Roslyn no soporta (aspx, razor, html, xml…) con el editor de VS.
    /// Secuencial en el hilo de UI; para cada archivo prueba las estrategias en orden:
    /// documento ya abierto → editor invisible → editor real (solo Web Forms con regeneración del designer).
    /// </summary>
    public class VsEditorFormatter : IEditorFormatter
    {
        private readonly IReadOnlyList<IEditorFormatStrategy> _strategies;
        private readonly ISettingsStore _settingsStore;

        public VsEditorFormatter(IEnumerable<IEditorFormatStrategy> strategies, ISettingsStore settingsStore)
        {
            _strategies = strategies.OrderBy(s => s.Order).ToList();
            _settingsStore = settingsStore;
        }

        public async Task<SweepSummary> FormatAsync(
            IReadOnlyList<string> filePaths,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            SweepSummary summary = new SweepSummary();
            SweepOptions options = await _settingsStore.GetOptionsAsync();

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
                    EditorFormatOutcome outcome = await FormatFileAsync(filePath, options, cancellationToken);
                    if (outcome == EditorFormatOutcome.NotHandled)
                    {
                        // Ninguna estrategia pudo sin abrir una ventana: se informa, no se abre nada.
                        summary.Failures.Add(new SweepFailure(filePath, Strings.Format(Strings.EditorCannotFormatInBackground, Path.GetExtension(filePath))));
                    }
                    else
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

        private async Task<EditorFormatOutcome> FormatFileAsync(string filePath, SweepOptions options, CancellationToken cancellationToken)
        {
            foreach (IEditorFormatStrategy strategy in _strategies)
            {
                EditorFormatOutcome outcome = await strategy.TryFormatAsync(filePath, options, cancellationToken);
                if (outcome != EditorFormatOutcome.NotHandled)
                    return outcome;
            }

            return EditorFormatOutcome.NotHandled;
        }
    }
}
