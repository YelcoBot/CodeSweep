using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Services;

namespace YelcoBot.CodeSweep.Domain.Routing
{
    public sealed class DocumentRouter
    {
        private readonly GeneratedCodeDetector _generatedCodeDetector;
        private readonly Func<string, bool> _fileExists;

        public DocumentRouter(GeneratedCodeDetector generatedCodeDetector, Func<string, bool>? fileExists = null)
        {
            _generatedCodeDetector = generatedCodeDetector;
            _fileExists = fileExists ?? File.Exists;
        }

        public CleanupEngine Route(string filePath, SweepOptions options)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return CleanupEngine.Skip;

            if (options.IgnoreGeneratedCode && _generatedCodeDetector.IsGeneratedCode(filePath))
                return CleanupEngine.Skip;

            if (IsExcludedByPattern(filePath, options.ExcludePatterns))
                return CleanupEngine.Skip;

            if (options.ExcludeT4GeneratedCode && IsT4Generated(filePath))
                return CleanupEngine.Skip;

            string extension = Path.GetExtension(filePath);

            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                return options.IncludeCSharp ? CleanupEngine.Roslyn : CleanupEngine.Skip;

            if (extension.Equals(".vb", StringComparison.OrdinalIgnoreCase))
                return options.IncludeVisualBasic ? CleanupEngine.Roslyn : CleanupEngine.Skip;

            if (options.FormatEditorFiles && FileTypeGroups.GetEditorExtensions(options).Contains(extension))
                return CleanupEngine.Editor;

            return CleanupEngine.Skip;
        }

        public static HashSet<string> ParseExtensions(string? extensions)
        {
            IEnumerable<string> items = (extensions ?? string.Empty)
                .Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Select(e => e.StartsWith(".") ? e : "." + e);

            return new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsExcludedByPattern(string filePath, string? patterns)
        {
            foreach (string pattern in (patterns ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    if (Regex.IsMatch(filePath, pattern.Trim(), RegexOptions.IgnoreCase))
                        return true;
                }
                catch (ArgumentException)
                {
                    // Regex mal escrita en las opciones: se ignora esa entrada.
                }
            }

            return false;
        }

        /// <summary>Generado por T4: existe una plantilla .tt con el mismo nombre (Model.tt → Model.cs).</summary>
        private bool IsT4Generated(string filePath)
        {
            return _fileExists(Path.ChangeExtension(filePath, ".tt"));
        }
    }
}
