using System;
using System.Collections.Generic;

namespace YelcoBot.CodeSweep.Domain.Results
{
    public sealed class SweepSummary
    {
        public int ProcessedFilesCount { get; set; }
        public int ChangedFilesCount { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsCancelled { get; set; }
        public List<SweepFailure> Failures { get; } = new();

        public static SweepSummary Cancelled() => new() { IsCancelled = true };
    }
}
