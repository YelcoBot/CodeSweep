namespace YelcoBot.CodeSweep.Domain.Results
{
    public sealed class SweepFailure
    {
        public string FilePath { get; }

        public string ErrorMessage { get; }

        public SweepFailure(string filePath, string errorMessage)
        {
            FilePath = filePath;
            ErrorMessage = errorMessage;
        }
    }
}
