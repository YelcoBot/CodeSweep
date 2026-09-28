using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.UseCases
{
    public class CleanupOpenDocumentsUseCase
    {
        private readonly ICodeCleaner _codeCleaner;
        private readonly ISettingsStore _settingsStore;
        private readonly IUserInteraction _userInteraction;

        public CleanupOpenDocumentsUseCase(
            ICodeCleaner codeCleaner,
            ISettingsStore settingsStore,
            IUserInteraction userInteraction)
        {
            _codeCleaner = codeCleaner;
            _settingsStore = settingsStore;
            _userInteraction = userInteraction;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            SweepOptions options = await _settingsStore.GetOptionsAsync();
            DocumentSelection selection = DocumentSelection.OpenDocuments();

            SweepSummary summary = new SweepSummary();
            await _userInteraction.RunWithProgressAsync("Cleaning Open Documents...", async (progress, ct) =>
            {
                summary = await _codeCleaner.CleanAsync(selection, options, progress, ct);
            });

            await _userInteraction.ShowSummaryAsync(summary);
        }
    }
}
