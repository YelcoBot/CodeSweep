using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    public interface ISettingsStore
    {
        Task<SweepOptions> GetOptionsAsync();

        Task SaveOptionsAsync(SweepOptions options);
    }
}
