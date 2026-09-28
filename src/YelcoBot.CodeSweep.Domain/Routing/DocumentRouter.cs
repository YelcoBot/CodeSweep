using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Services;

namespace YelcoBot.CodeSweep.Domain.Routing
{
    public sealed class DocumentRouter
    {
        private static readonly HashSet<string> RoslynExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".vb"
        };

        private readonly GeneratedCodeDetector _generatedCodeDetector;

        public DocumentRouter(GeneratedCodeDetector generatedCodeDetector)
        {
            _generatedCodeDetector = generatedCodeDetector;
        }

        public CleanupEngine Route(string filePath, SweepOptions options)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return CleanupEngine.Skip;

            if (options.IgnoreGeneratedCode && _generatedCodeDetector.IsGeneratedCode(filePath))
                return CleanupEngine.Skip;

            string extension = Path.GetExtension(filePath);

            if (RoslynExtensions.Contains(extension))
                return CleanupEngine.Roslyn;

            if (options.FormatEditorFiles && ParseExtensions(options.EditorFileExtensions).Contains(extension))
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
    }
}
