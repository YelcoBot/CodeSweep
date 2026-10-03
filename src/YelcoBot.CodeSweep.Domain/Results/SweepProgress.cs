namespace YelcoBot.CodeSweep.Domain.Results
{
    public sealed class SweepProgress
    {
        public int Current { get; }

        public int Total { get; }

        public string CurrentFilePath { get; }

        public SweepProgress(int current, int total, string currentFilePath)
        {
            Current = current;
            Total = total;
            CurrentFilePath = currentFilePath;
        }
    }
}
