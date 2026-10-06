namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions
{
    /// <summary>
    /// Access to C#/VB files that belong to no loaded project (a folder opened without a solution, stray files).
    /// They are not in the Roslyn workspace, so they are read and written on their own.
    /// </summary>
    public interface ILooseFileStore
    {
        /// <summary>Opens the file from its editor when open, otherwise from disk. Returns null when it does not exist.</summary>
        Task<ILooseFile?> OpenAsync(string filePath, CancellationToken cancellationToken = default);
    }

    public interface ILooseFile
    {
        string Text { get; }

        /// <summary>Applies the cleaned text. Returns false when the user kept typing in the meantime.</summary>
        Task<bool> ApplyAsync(string cleanedText, CancellationToken cancellationToken = default);
    }
}
