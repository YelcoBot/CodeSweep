namespace YelcoBot.CodeSweep.Domain.Results
{
    public sealed class SweepSummary
    {
        public int ProcessedFilesCount { get; set; }

        public int ChangedFilesCount { get; set; }

        public TimeSpan Duration { get; set; }

        public bool IsCancelled { get; set; }

        public List<SweepFailure> Failures { get; } = new();

        public List<string> ChangedFilePaths { get; } = new();

        public static SweepSummary Cancelled() => new() { IsCancelled = true };

        public void Merge(SweepSummary other)
        {
            ProcessedFilesCount += other.ProcessedFilesCount;
            ChangedFilesCount += other.ChangedFilesCount;
            Duration += other.Duration;
            IsCancelled |= other.IsCancelled;
            Failures.AddRange(other.Failures);
            ChangedFilePaths.AddRange(other.ChangedFilePaths);
        }
    }
}
