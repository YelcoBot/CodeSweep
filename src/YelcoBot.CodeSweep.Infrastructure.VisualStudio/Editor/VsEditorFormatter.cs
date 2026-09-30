using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
    /// Lo que el editor invisible no pudo formatear se lista en un solo mensaje y el usuario decide si usar el editor.
    /// </summary>
    public class VsEditorFormatter : IEditorFormatter
    {
        private const int MaxFilesInMessage = 10;

        private readonly IReadOnlyList<IEditorFormatStrategy> _strategies;
        private readonly WindowFormatStrategy _windowStrategy;
        private readonly ISettingsStore _settingsStore;
        private readonly IUserInteraction _userInteraction;

        public VsEditorFormatter(
            IEnumerable<IEditorFormatStrategy> strategies,
            WindowFormatStrategy windowStrategy,
            ISettingsStore settingsStore,
            IUserInteraction userInteraction)
        {
            _strategies = strategies.OrderBy(s => s.Order).ToList();
            _windowStrategy = windowStrategy;
            _settingsStore = settingsStore;
            _userInteraction = userInteraction;
        }

        public async Task<SweepSummary> FormatAsync(
            IReadOnlyList<string> filePaths,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            SweepSummary summary = new SweepSummary();
            SweepOptions options = await _settingsStore.GetOptionsAsync();
            List<string> notFormatted = new List<string>();

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
                        notFormatted.Add(filePath);
                    }
                    else
                    {
                        Record(summary, filePath, outcome);
                    }
                }
                catch (Exception ex)
                {
                    summary.Failures.Add(new SweepFailure(filePath, ex.Message));
                }

                // Ceder el hilo de UI entre archivos para que VS siga respondiendo.
                await Task.Yield();
            }

            if (notFormatted.Count > 0 && !summary.IsCancelled)
            {
                await HandleNotFormattedAsync(notFormatted, summary, progress, cancellationToken);
            }

            stopwatch.Stop();
            summary.Duration = stopwatch.Elapsed;
            return summary;
        }

        /// <summary>
        /// Un solo mensaje con los archivos que no se pudieron formatear sin abrir el editor.
        /// Sí → abrir, formatear, guardar y cerrar cada uno. No → quedan en el resumen como no formateados.
        /// </summary>
        private async Task HandleNotFormattedAsync(List<string> files, SweepSummary summary, IProgress<SweepProgress>? progress, CancellationToken cancellationToken)
        {
            bool useEditor = await _userInteraction.ConfirmAsync(Strings.EditorAskTitle, BuildAskMessage(files));

            if (!useEditor)
            {
                summary.Failures.AddRange(files.Select(f =>
                    new SweepFailure(f, Strings.Format(Strings.EditorCannotFormatInBackground, Path.GetExtension(f)))));
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            for (int i = 0; i < files.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    summary.IsCancelled = true;
                    return;
                }

                string filePath = files[i];
                progress?.Report(new SweepProgress(i + 1, files.Count, filePath));

                try
                {
                    EditorFormatOutcome outcome = await _windowStrategy.FormatInEditorAsync(filePath, cancellationToken);
                    if (outcome == EditorFormatOutcome.NotHandled)
                    {
                        summary.Failures.Add(new SweepFailure(filePath, Strings.EditorCannotFormat));
                    }
                    else
                    {
                        Record(summary, filePath, outcome);
                    }
                }
                catch (Exception ex)
                {
                    summary.Failures.Add(new SweepFailure(filePath, ex.Message));
                }

                await Task.Yield();
            }
        }

        private static string BuildAskMessage(List<string> files)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine(Strings.Format(Strings.EditorAskHeader, files.Count));
            message.AppendLine();

            foreach (string file in files.Take(MaxFilesInMessage))
            {
                message.AppendLine("  • " + Path.GetFileName(file));
            }

            if (files.Count > MaxFilesInMessage)
            {
                message.AppendLine(Strings.Format(Strings.SelectedItemsMore, files.Count - MaxFilesInMessage));
            }

            message.AppendLine();
            message.Append(Strings.EditorAskQuestion);
            return message.ToString();
        }

        private static void Record(SweepSummary summary, string filePath, EditorFormatOutcome outcome)
        {
            summary.ProcessedFilesCount++;

            if (outcome == EditorFormatOutcome.Changed)
            {
                summary.ChangedFilesCount++;
                summary.ChangedFilePaths.Add(filePath);
            }
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
