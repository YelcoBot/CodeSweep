using System;
using System.Collections.Generic;
using System.Linq;

namespace YelcoBot.CodeSweep.Domain.Selection
{
    public enum DocumentSelectionType
    {
        ActiveDocument,
        OpenDocuments,
        Solution,
        Files
    }

    public sealed class DocumentSelection
    {
        public DocumentSelectionType Type { get; }
        public IReadOnlyList<string> FilePaths { get; }

        private DocumentSelection(DocumentSelectionType type, IReadOnlyList<string>? filePaths = null)
        {
            Type = type;
            FilePaths = filePaths ?? Array.Empty<string>();
        }

        public static DocumentSelection ActiveDocument() => new(DocumentSelectionType.ActiveDocument);
        public static DocumentSelection OpenDocuments() => new(DocumentSelectionType.OpenDocuments);
        public static DocumentSelection Solution() => new(DocumentSelectionType.Solution);
        public static DocumentSelection File(string filePath) => new(DocumentSelectionType.Files, new[] { filePath });
        public static DocumentSelection Files(IEnumerable<string> filePaths) => new(DocumentSelectionType.Files, filePaths.ToList());
    }
}
