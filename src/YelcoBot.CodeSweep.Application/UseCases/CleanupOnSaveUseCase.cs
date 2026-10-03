using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.Application.Services;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.UseCases
{
    public class CleanupOnSaveUseCase
    {
        private readonly CleanupOrchestrator _orchestrator;
        private readonly ISettingsStore _settingsStore;
        private readonly SweepActivity _activity;
        private readonly ISweepLog _log;

        public CleanupOnSaveUseCase(
            CleanupOrchestrator orchestrator,
            ISettingsStore settingsStore,
            SweepActivity activity,
            ISweepLog log)
        {
            _orchestrator = orchestrator;
            _settingsStore = settingsStore;
            _activity = activity;
            _log = log;
        }

        public async Task ExecuteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            // Los guardados hechos por un cleanup en curso no deben disparar otro cleanup.
            if (_activity.IsRunning || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            SweepOptions options = await _settingsStore.GetOptionsAsync();
            if (!options.CleanupOnSave)
            {
                return;
            }

            // Output → CodeSweep: si el archivo se formatea al guardar y aquí no aparece, no fue CodeSweep.
            await _log.WriteLineAsync(Strings.Format(Strings.LogCleanupOnSave, filePath));

            DocumentSelection selection = DocumentSelection.File(filePath);
            await _orchestrator.ExecuteAsync(selection, options, null, cancellationToken);
        }
    }
}
