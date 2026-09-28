using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.UseCases
{
    public class CleanupOnSaveUseCase
    {
        private readonly ICodeCleaner _codeCleaner;
        private readonly ISettingsStore _settingsStore;

        public CleanupOnSaveUseCase(
            ICodeCleaner codeCleaner,
            ISettingsStore settingsStore)
        {
            _codeCleaner = codeCleaner;
            _settingsStore = settingsStore;
        }

        public async Task ExecuteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            SweepOptions options = await _settingsStore.GetOptionsAsync();
            if (!options.CleanupOnSave || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            DocumentSelection selection = DocumentSelection.File(filePath);
            await _codeCleaner.CleanAsync(selection, options, null, cancellationToken);
        }
    }
}
