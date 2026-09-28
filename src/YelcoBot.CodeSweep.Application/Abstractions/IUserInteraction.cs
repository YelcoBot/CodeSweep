using System;
using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    public interface IUserInteraction
    {
        Task<bool> ConfirmAsync(string title, string message);
        Task ShowSummaryAsync(SweepSummary summary);
        Task RunWithProgressAsync(string title, Func<IProgress<SweepProgress>, CancellationToken, Task> action);
    }
}
