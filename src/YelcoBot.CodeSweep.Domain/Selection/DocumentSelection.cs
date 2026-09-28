namespace YelcoBot.CodeSweep.Domain.Selection
{
    public enum DocumentSelectionType
    {
        ActiveDocument,
        OpenDocuments,
        Solution,
        SpecificFile
    }

    public sealed class DocumentSelection
    {
        public DocumentSelectionType Type { get; }
        public string? FilePath { get; }

        private DocumentSelection(DocumentSelectionType type, string? filePath = null)
        {
            Type = type;
            FilePath = filePath;
        }

        public static DocumentSelection ActiveDocument() => new(DocumentSelectionType.ActiveDocument);
        public static DocumentSelection OpenDocuments() => new(DocumentSelectionType.OpenDocuments);
        public static DocumentSelection Solution() => new(DocumentSelectionType.Solution);
        public static DocumentSelection File(string filePath) => new(DocumentSelectionType.SpecificFile, filePath);
    }
}
