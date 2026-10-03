using System.Diagnostics;
using System.IO;
using System.Text;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Services.Whitespace;
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
                    await CleanFileAsync(filePath, options, summary, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    summary.Failures.Add(new SweepFailure(filePath, ex.Message));
                }
            }

            summary.Duration = stopwatch.Elapsed;
            return summary;
        }

        private async Task CleanFileAsync(string filePath, SweepOptions options, SweepSummary summary, CancellationToken cancellationToken)
        {
            ITextBuffer? openBuffer = await GetOpenBufferAsync(filePath);

            string original;
            int version = 0;
            FileContent? file = null;

            if (openBuffer != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ITextSnapshot snapshot = openBuffer.CurrentSnapshot;
                original = snapshot.GetText();
                version = snapshot.Version.VersionNumber;
            }
            else
            {
                if (!File.Exists(filePath))
                    return;

                file = FileContent.Read(filePath);
                original = file.Text;
            }

            // Formato + reglas universales, fuera del hilo de UI.
            (string cleaned, string? reason) = await Task.Run(() => Clean(original, filePath, options), cancellationToken);

            if (reason != null)
            {
                summary.Failures.Add(new SweepFailure(filePath, reason));
            }

            summary.ProcessedFilesCount++;
            if (cleaned == original)
                return;

            if (openBuffer != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // El usuario siguió escribiendo mientras se calculaba: no pisar sus cambios.
                if (openBuffer.CurrentSnapshot.Version.VersionNumber != version)
                    return;

                ReplaceChangedRegion(openBuffer, original, cleaned);
            }
            else
            {
                file!.Write(cleaned);
            }

            summary.ChangedFilesCount++;
            summary.ChangedFilePaths.Add(filePath);
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

        /// <summary>Reemplaza solo lo que cambió (sin el prefijo y sufijo comunes): el cursor y el scroll se mueven lo mínimo.</summary>
        private static void ReplaceChangedRegion(ITextBuffer buffer, string original, string cleaned)
        {
            int prefix = 0;
            int maxPrefix = Math.Min(original.Length, cleaned.Length);
            while (prefix < maxPrefix && original[prefix] == cleaned[prefix])
            {
                prefix++;
            }

            int suffix = 0;
            int maxSuffix = Math.Min(original.Length, cleaned.Length) - prefix;
            while (suffix < maxSuffix && original[original.Length - 1 - suffix] == cleaned[cleaned.Length - 1 - suffix])
            {
                suffix++;
            }

            using (ITextEdit edit = buffer.CreateEdit())
            {
                edit.Replace(new Span(prefix, original.Length - prefix - suffix), cleaned.Substring(prefix, cleaned.Length - prefix - suffix));
                edit.Apply();
            }
        }

        private static async Task<ITextBuffer?> GetOpenBufferAsync(string filePath)
        {
            try
            {
                if (!await VS.Documents.IsOpenAsync(filePath))
                    return null;

                DocumentView? view = await VS.Documents.GetDocumentViewAsync(filePath);
                return view?.TextBuffer;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>Texto de un archivo cerrado y cómo volver a escribirlo igual (codificación y BOM).</summary>
        private sealed class FileContent
        {
            private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);

            private readonly string _path;
            private readonly Encoding _encoding;

            private FileContent(string path, string text, Encoding encoding)
            {
                _path = path;
                Text = text;
                _encoding = encoding;
            }

            public string Text { get; }

            public static FileContent Read(string path)
            {
                byte[] bytes = File.ReadAllBytes(path);

                if (StartsWith(bytes, 0xEF, 0xBB, 0xBF))
                    return new FileContent(path, new UTF8Encoding(true).GetString(bytes, 3, bytes.Length - 3), new UTF8Encoding(true));

                if (StartsWith(bytes, 0xFF, 0xFE))
                    return new FileContent(path, Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), Encoding.Unicode);

                if (StartsWith(bytes, 0xFE, 0xFF))
                    return new FileContent(path, Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), Encoding.BigEndianUnicode);

                try
                {
                    return new FileContent(path, StrictUtf8.GetString(bytes), new UTF8Encoding(false));
                }
                catch (DecoderFallbackException)
                {
                    // Sin BOM y no es UTF-8 válido: la página de códigos de Windows (lo que usan los scripts viejos).
                    return new FileContent(path, Encoding.Default.GetString(bytes), Encoding.Default);
                }
            }

            /// <summary>Escribe con la misma codificación; GetPreamble agrega el BOM solo si el original lo tenía.</summary>
            public void Write(string text)
            {
                byte[] preamble = _encoding.GetPreamble();
                byte[] body = _encoding.GetBytes(text);

                using (FileStream stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    stream.Write(preamble, 0, preamble.Length);
                    stream.Write(body, 0, body.Length);
                }
            }

            private static bool StartsWith(byte[] bytes, params byte[] prefix)
            {
                return bytes.Length >= prefix.Length && !prefix.Where((b, i) => bytes[i] != b).Any();
            }
        }
    }
}
