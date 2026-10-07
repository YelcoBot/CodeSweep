using System.IO;
using System.Text;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// The text of a file and the way to write it back cleaned, without opening editors:
    /// an open file (VS or an SSMS query window) is edited in its buffer in a single edit and the user saves;
    /// a closed file is rewritten on disk with the encoding (and BOM) it had.
    /// </summary>
    internal sealed class TextFileSession : ILooseFile
    {
        private readonly ITextBuffer? _openBuffer;
        private readonly int _version;
        private readonly FileContent? _file;

        private TextFileSession(string text, ITextBuffer? openBuffer, int version, FileContent? file)
        {
            Text = text;
            _openBuffer = openBuffer;
            _version = version;
            _file = file;
        }

        public string Text { get; }

        /// <summary>Opens the file. Returns null when it is neither open nor on disk.</summary>
        public static async Task<TextFileSession?> OpenAsync(string filePath, CancellationToken cancellationToken)
        {
            ITextBuffer? openBuffer = await GetOpenBufferAsync(filePath, cancellationToken);

            if (openBuffer != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ITextSnapshot snapshot = openBuffer.CurrentSnapshot;
                return new TextFileSession(snapshot.GetText(), openBuffer, snapshot.Version.VersionNumber, null);
            }

            await TaskScheduler.Default;

            if (!File.Exists(filePath))
                return null;

            FileContent file = FileContent.Read(filePath);
            return new TextFileSession(file.Text, null, 0, file);
        }

        /// <summary>
        /// Applies the cleaned text. Returns false, leaving the buffer untouched,
        /// when the user edited it after it was read.
        /// </summary>
        public async Task<bool> ApplyAsync(string cleanedText, CancellationToken cancellationToken = default)
        {
            if (_openBuffer != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                bool editedMeanwhile = _openBuffer.CurrentSnapshot.Version.VersionNumber != _version;
                if (editedMeanwhile)
                    return false;

                ReplaceChangedRegion(_openBuffer, Text, cleanedText);
            }
            else
            {
                _file!.Write(cleanedText);
            }

            return true;
        }

        /// <summary>
        /// Replaces only what changed (skipping the common prefix and suffix),
        /// so the caret and the scroll position move as little as possible.
        /// </summary>
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

        private static async Task<ITextBuffer?> GetOpenBufferAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                if (!await VS.Documents.IsOpenAsync(filePath))
                    return null;

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                DocumentView? view = await VS.Documents.GetDocumentViewAsync(filePath);
                return view?.TextBuffer;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>The text of a closed file and the way to write it back unchanged (encoding and BOM).</summary>
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

            /// <summary>
            /// Reads the file detecting its encoding: by BOM, then strict UTF-8,
            /// and finally the Windows code page (what legacy scripts use).
            /// </summary>
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
                    return new FileContent(path, Encoding.Default.GetString(bytes), Encoding.Default);
                }
            }

            /// <summary>Writes with the original encoding; the BOM is added only when the file had one.</summary>
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

    /// <summary>C#/VB files outside any project, accessed like .sql files: the open buffer or the disk.</summary>
    public sealed class VsLooseFileStore : ILooseFileStore
    {
        public async Task<ILooseFile?> OpenAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await TextFileSession.OpenAsync(filePath, cancellationToken);
        }
    }
}
