using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Services;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Routing;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.UseCases
{
    /// <summary>
    /// Limpia lo seleccionado en el Explorador de soluciones: solución, proyectos, carpetas o archivos (recursivo).
    /// Si la selección incluye algún contenedor, pide confirmación listándolos todos en un solo mensaje.
    /// </summary>
    public class CleanupSelectedItemsUseCase
    {
        private const int MaxContainersInMessage = 10;

        private readonly CleanupOrchestrator _orchestrator;
        private readonly IDocumentProvider _documentProvider;
        private readonly DocumentRouter _documentRouter;
        private readonly ISettingsStore _settingsStore;
        private readonly IUserInteraction _userInteraction;

        public CleanupSelectedItemsUseCase(
            CleanupOrchestrator orchestrator,
            IDocumentProvider documentProvider,
            DocumentRouter documentRouter,
            ISettingsStore settingsStore,
            IUserInteraction userInteraction)
        {
            _orchestrator = orchestrator;
            _documentProvider = documentProvider;
            _documentRouter = documentRouter;
            _settingsStore = settingsStore;
            _userInteraction = userInteraction;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            SweepOptions options = await _settingsStore.GetOptionsAsync();

            SolutionExplorerSelectionInfo selectionInfo = await _documentProvider.DescribeSolutionExplorerSelectionAsync(cancellationToken);
            if (selectionInfo.HasContainers && !await ConfirmContainersAsync(selectionInfo, options))
            {
                return;
            }

            DocumentSelection selection = DocumentSelection.SolutionExplorerSelection();

            SweepSummary summary = new SweepSummary();
            await _userInteraction.RunWithProgressAsync("Cleaning Selected Items...", async (progress, ct) =>
            {
                summary = await _orchestrator.ExecuteAsync(selection, options, progress, ct);
            });

            await _userInteraction.ShowSummaryAsync(summary);
        }

        private Task<bool> ConfirmContainersAsync(SolutionExplorerSelectionInfo selectionInfo, SweepOptions options)
        {
            // Solo cuenta lo que CodeSweep realmente procesaría según File Types.
            List<(SelectedContainer Container, int Files)> containers = selectionInfo.Containers
                .Select(c => (c, c.FilePaths.Count(p => _documentRouter.Route(p, options) != CleanupEngine.Skip)))
                .ToList();

            int totalFiles = containers.Sum(c => c.Files);

            StringBuilder message = new StringBuilder();
            message.AppendLine(selectionInfo.SelectedFileCount > 0
                ? "Your selection includes files and also these containers:"
                : "Your selection includes these containers:");
            message.AppendLine();

            foreach ((SelectedContainer container, int files) in containers.Take(MaxContainersInMessage))
            {
                message.AppendLine($"  • {container.Kind}  {container.Name}  —  {files} file(s)");
            }

            if (containers.Count > MaxContainersInMessage)
            {
                message.AppendLine($"  …and {containers.Count - MaxContainersInMessage} more");
            }

            message.AppendLine();
            message.AppendLine(selectionInfo.SelectedFileCount > 0
                ? $"All their files will be cleaned ({totalFiles} file(s)), not only the selected ones."
                : $"All their files will be cleaned ({totalFiles} file(s)).");
            message.Append("Continue?");

            return _userInteraction.ConfirmAsync("CodeSweep: Cleanup Selected Code", message.ToString());
        }
    }
}
