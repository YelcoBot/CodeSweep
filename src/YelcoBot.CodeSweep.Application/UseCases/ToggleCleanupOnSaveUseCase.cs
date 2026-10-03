using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Application.UseCases
{
    public class ToggleCleanupOnSaveUseCase
    {
        private readonly ISettingsStore _settingsStore;

        public ToggleCleanupOnSaveUseCase(ISettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
        }

        public async Task<bool> ExecuteAsync()
        {
            SweepOptions options = await _settingsStore.GetOptionsAsync();
            options.CleanupOnSave = !options.CleanupOnSave;
            await _settingsStore.SaveOptionsAsync(options);
            return options.CleanupOnSave;
        }
    }
}
