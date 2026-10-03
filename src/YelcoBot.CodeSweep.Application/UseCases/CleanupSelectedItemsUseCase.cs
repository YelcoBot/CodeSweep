using System.Text;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Localization;
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

            // Sin contenedores: lo seleccionado. Con contenedores: Sí = completo, No = solo archivos sueltos, Cancelar = nada.
            DocumentSelection selection = DocumentSelection.SolutionExplorerSelection();
            if (selectionInfo.HasContainers)
            {
                UserChoice choice = await AskAboutContainersAsync(selectionInfo, options);
                if (choice == UserChoice.Cancel)
                {
                    return;
                }

                if (choice == UserChoice.No)
                {
                    selection = DocumentSelection.Files(selectionInfo.SelectedFilePaths);
                }
            }

            SweepSummary summary = new SweepSummary();
            await _userInteraction.RunWithProgressAsync(Strings.ProgressSelectedItems, async (progress, ct) =>
            {
                summary = await _orchestrator.ExecuteAsync(selection, options, progress, ct);
            });

            await _userInteraction.ShowSummaryAsync(summary);
        }

        private async Task<UserChoice> AskAboutContainersAsync(SolutionExplorerSelectionInfo selectionInfo, SweepOptions options)
        {
            string title = Strings.SelectedItemsTitle;
            string message = BuildContainersMessage(selectionInfo, options);

            // Solo contenedores: no hay "solo archivos" posible → Sí / No (No = cancelar).
            if (selectionInfo.SelectedFileCount == 0)
            {
                return await _userInteraction.ConfirmAsync(title, message + Strings.CommonContinue) ? UserChoice.Yes : UserChoice.Cancel;
            }

            int selectedFiles = selectionInfo.SelectedFilePaths.Count(p => _documentRouter.Route(p, options) != CleanupEngine.Skip);
            return await _userInteraction.AskYesNoCancelAsync(title,
                message +
                Strings.SelectedItemsOptionYes + "\n" +
                Strings.Format(Strings.SelectedItemsOptionNo, selectedFiles) + "\n" +
                Strings.SelectedItemsOptionCancel);
        }

        private string BuildContainersMessage(SolutionExplorerSelectionInfo selectionInfo, SweepOptions options)
        {
            // Solo cuenta lo que CodeSweep realmente procesaría según File Types.
            List<(SelectedContainer Container, int Files)> containers = selectionInfo.Containers
                .Select(c => (c, c.FilePaths.Count(p => _documentRouter.Route(p, options) != CleanupEngine.Skip)))
                .ToList();

            int totalFiles = containers.Sum(c => c.Files);

            StringBuilder message = new StringBuilder();
            message.AppendLine(selectionInfo.SelectedFileCount > 0
                ? Strings.SelectedItemsIncludesFilesAndContainers
                : Strings.SelectedItemsIncludesContainers);
            message.AppendLine();

            foreach ((SelectedContainer container, int files) in containers.Take(MaxContainersInMessage))
            {
                message.AppendLine(Strings.Format(Strings.SelectedItemsContainerLine, container.Kind, container.Name, files));
            }

            if (containers.Count > MaxContainersInMessage)
            {
                message.AppendLine(Strings.Format(Strings.SelectedItemsMore, containers.Count - MaxContainersInMessage));
            }

            message.AppendLine();
            message.AppendLine(Strings.Format(Strings.SelectedItemsContainersTotal, totalFiles));
            message.AppendLine();

            return message.ToString();
        }
    }
}
