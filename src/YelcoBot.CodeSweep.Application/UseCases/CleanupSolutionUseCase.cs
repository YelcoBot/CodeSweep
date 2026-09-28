using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Services;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.UseCases
{
    public class CleanupSolutionUseCase
    {
        private readonly CleanupOrchestrator _orchestrator;
        private readonly ISettingsStore _settingsStore;
        private readonly IUserInteraction _userInteraction;

        public CleanupSolutionUseCase(
            CleanupOrchestrator orchestrator,
            ISettingsStore settingsStore,
            IUserInteraction userInteraction)
        {
            _orchestrator = orchestrator;
            _settingsStore = settingsStore;
            _userInteraction = userInteraction;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            bool confirmed = await _userInteraction.ConfirmAsync(
                "Cleanup Entire Solution",
                "Are you sure you want to run CodeSweep on all documents in the solution?");

            if (!confirmed)
            {
                return;
            }

            SweepOptions options = await _settingsStore.GetOptionsAsync();
            DocumentSelection selection = DocumentSelection.Solution();

            SweepSummary summary = new SweepSummary();
            await _userInteraction.RunWithProgressAsync("Cleaning Solution...", async (progress, ct) =>
            {
                summary = await _orchestrator.ExecuteAsync(selection, options, progress, ct);
            });

            await _userInteraction.ShowSummaryAsync(summary);
        }
    }
}
