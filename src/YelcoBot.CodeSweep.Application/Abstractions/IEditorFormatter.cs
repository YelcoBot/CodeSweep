using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    public interface IEditorFormatter
    {
        Task<SweepSummary> FormatAsync(
            IReadOnlyList<string> filePaths,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
