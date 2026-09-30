using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Services;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.UseCases
{
    public class CleanupActiveDocumentUseCase
    {
        private readonly CleanupOrchestrator _orchestrator;
        private readonly IEditorContext _editorContext;
        private readonly ISettingsStore _settingsStore;
        private readonly IUserInteraction _userInteraction;

        public CleanupActiveDocumentUseCase(
            CleanupOrchestrator orchestrator,
            IEditorContext editorContext,
            ISettingsStore settingsStore,
            IUserInteraction userInteraction)
        {
            _orchestrator = orchestrator;
            _editorContext = editorContext;
            _settingsStore = settingsStore;
            _userInteraction = userInteraction;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            string? activePath = await _editorContext.GetActiveDocumentPathAsync();
            if (string.IsNullOrWhiteSpace(activePath))
            {
                return;
            }

            SweepOptions options = await _settingsStore.GetOptionsAsync();
            DocumentSelection selection = DocumentSelection.File(activePath!);

            SweepSummary summary = new SweepSummary();
            await _userInteraction.RunWithProgressAsync(Strings.ProgressActiveDocument, async (progress, ct) =>
            {
                summary = await _orchestrator.ExecuteAsync(selection, options, progress, ct);
            });

            await _userInteraction.ShowSummaryAsync(summary);
        }
    }
}
