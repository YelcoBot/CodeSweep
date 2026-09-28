using System;
using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    public interface ICodeCleaner
    {
        Task<SweepSummary> CleanAsync(
            DocumentSelection selection,
            SweepOptions options,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
