using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Services.Whitespace;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor;
using TextEdit = YelcoBot.CodeSweep.Domain.Services.Whitespace.TextEdit;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Sql
{
    /// <summary>
    /// Limpia .sql sin abrir editores: formato con ScriptDOM (si está activo) y luego las reglas universales.
    /// El cálculo corre en segundo plano y en paralelo; el resultado se aplica:
    /// - Archivo abierto (VS o ventana de consulta de SSMS): en su buffer, en una sola edición. El usuario guarda.
    /// - Archivo cerrado: en disco, con la misma codificación (y BOM) que tenía.
    /// </summary>
    public class VsSqlCleaner : ISqlCleaner
    {
        private const string Extension = ".sql";

        private readonly ISqlFormatter _formatter;

        public VsSqlCleaner(ISqlFormatter formatter)
        {
            _formatter = formatter;
        }

        public async Task<SweepSummary> CleanAsync(
            IReadOnlyList<string> filePaths,
            SweepOptions options,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            SweepSummary summary = new SweepSummary();
            ConcurrentBag<SweepSummary> results = new ConcurrentBag<SweepSummary>();
            int started = 0;

            using (SemaphoreSlim throttler = new SemaphoreSlim(Environment.ProcessorCount))
            {
                IEnumerable<Task> tasks = filePaths.Select(filePath => Task.Run(async () =>
                {
                    await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        progress?.Report(new SweepProgress(Interlocked.Increment(ref started), filePaths.Count, filePath));
                        results.Add(await CleanFileAsync(filePath, options, cancellationToken).ConfigureAwait(false));
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
                    summary.IsCancelled = true;
                }
            }

            foreach (SweepSummary result in results)
            {
                summary.Merge(result);
            }

            summary.IsCancelled |= cancellationToken.IsCancellationRequested;
            summary.Duration = stopwatch.Elapsed;
            return summary;
        }

        private async Task<SweepSummary> CleanFileAsync(string filePath, SweepOptions options, CancellationToken cancellationToken)
        {
            SweepSummary result = new SweepSummary();

            try
            {
                TextFileSession? file = await TextFileSession.OpenAsync(filePath, cancellationToken);
                if (file == null)
                    return result;

                string original = file.Text;

                // Formato + reglas universales, fuera del hilo de UI.
                (string cleaned, string? reason) = await Task.Run(() => Clean(original, filePath, options), cancellationToken);

                if (reason != null)
                {
                    result.Failures.Add(new SweepFailure(filePath, reason));
                }

                result.ProcessedFilesCount++;

                if (cleaned != original && await file.ApplyAsync(cleaned, cancellationToken))
                {
                    result.ChangedFilesCount++;
                    result.ChangedFilePaths.Add(filePath);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.Failures.Add(new SweepFailure(filePath, ex.Message));
            }

            return result;
        }

        private (string Text, string? Reason) Clean(string text, string filePath, SweepOptions options)
        {
            string? reason = null;

            if (options.EnableFormatSql && _formatter.TryFormat(text, filePath, options.SqlFormatter, out string formatted, out reason))
            {
                text = formatted;
            }

            if (WhitespaceCleaner.IsAnyEnabled(options))
            {
                text = ApplyWhitespaceRules(text, options);
            }

            return (text, reason);
        }

        private static string ApplyWhitespaceRules(string text, SweepOptions options)
        {
            List<TextLineInfo> lines = SplitLines(text);
            EditorTextGuard guard = EditorTextGuard.Create(Extension, lines.Select(l => l.Text).ToList());
            IReadOnlyList<TextEdit> edits = WhitespaceCleaner.FindEdits(lines, guard, options);

            StringBuilder result = new StringBuilder(text);
            foreach (TextEdit edit in edits.Reverse())
            {
                result.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.NewText);
            }

            return result.ToString();
        }

        private static List<TextLineInfo> SplitLines(string text)
        {
            List<TextLineInfo> lines = new List<TextLineInfo>();
            int start = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '\r' && text[i] != '\n')
                    continue;

                int breakLength = text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1;
                lines.Add(new TextLineInfo(start, text.Substring(start, i - start), text.Substring(i, breakLength)));
                i += breakLength - 1;
                start = i + 1;
            }

            lines.Add(new TextLineInfo(start, text.Substring(start), string.Empty));
            return lines;
        }
    }
}
