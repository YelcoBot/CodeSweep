using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Services;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.UseCases
{
    /// <summary>
    /// Limpia lo seleccionado en el Explorador de soluciones: solución, proyectos, carpetas o archivos (recursivo).
    /// </summary>
    public class CleanupSelectedItemsUseCase
    {
        private readonly CleanupOrchestrator _orchestrator;
        private readonly ISettingsStore _settingsStore;
        private readonly IUserInteraction _userInteraction;

        public CleanupSelectedItemsUseCase(
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
            SweepOptions options = await _settingsStore.GetOptionsAsync();
            DocumentSelection selection = DocumentSelection.SolutionExplorerSelection();

            SweepSummary summary = new SweepSummary();
            await _userInteraction.RunWithProgressAsync("Cleaning Selected Items...", async (progress, ct) =>
            {
                summary = await _orchestrator.ExecuteAsync(selection, options, progress, ct);
            });

            await _userInteraction.ShowSummaryAsync(summary);
        }
    }
}
