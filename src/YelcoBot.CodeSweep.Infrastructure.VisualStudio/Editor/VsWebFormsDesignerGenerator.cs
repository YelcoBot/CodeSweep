using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// Regenera los .designer.cs de Web Forms con el comando "Convert to Web Application" de VS
    /// (clic derecho en un .aspx de un proyecto Web Application). No hay API pública para esto:
    /// se seleccionan los archivos en el Explorador de soluciones y se ejecuta el comando, una vez por proyecto.
    /// </summary>
    public class VsWebFormsDesignerGenerator : IWebFormsDesignerGenerator
    {
        private const string ConvertToWebApplicationCommand = "Project.ConverttoWebApplication";

        public async Task<IReadOnlyList<SweepFailure>> RegenerateAsync(IReadOnlyList<string> filePaths, CancellationToken cancellationToken = default)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            List<SweepFailure> failures = new List<SweepFailure>();
            DTE2 dte = await VS.GetRequiredServiceAsync<DTE, DTE2>();
            IVsUIHierarchyWindow? solutionExplorer = VsShellUtilities.GetUIHierarchyWindow(
                ServiceProvider.GlobalProvider, VSConstants.StandardToolWindows.SolutionExplorer);

            if (solutionExplorer == null)
            {
                failures.Add(new SweepFailure("(designer)", "Solution Explorer is not available."));
                return failures;
            }

            // Ubicar cada archivo en su proyecto (hierarchy + itemid).
            List<(string Path, IVsUIHierarchy Hierarchy, uint ItemId)> items = new();
            foreach (string filePath in filePaths)
            {
                PhysicalFile? file = await PhysicalFile.FromFileAsync(filePath);
                if (file == null)
                {
                    failures.Add(new SweepFailure(filePath, "Designer not regenerated: the file is not part of the solution."));
                    continue;
                }

                file.GetItemInfo(out IVsHierarchy hierarchy, out uint itemId, out _);
                if (hierarchy is IVsUIHierarchy uiHierarchy)
                {
                    items.Add((filePath, uiHierarchy, itemId));
                }
            }

            // El comando actúa sobre la selección: un proyecto a la vez.
            foreach (IGrouping<IVsUIHierarchy, (string Path, IVsUIHierarchy Hierarchy, uint ItemId)> project in items.GroupBy(i => i.Hierarchy))
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool first = true;
                foreach ((string _, IVsUIHierarchy hierarchy, uint itemId) in project)
                {
                    EXPANDFLAGS flag = first ? EXPANDFLAGS.EXPF_SelectItem : EXPANDFLAGS.EXPF_AddSelectItem;
                    solutionExplorer.ExpandItem(hierarchy, itemId, flag);
                    first = false;
                }

                try
                {
                    Command command = dte.Commands.Item(ConvertToWebApplicationCommand);
                    if (!command.IsAvailable)
                    {
                        AddFailures(failures, project, "Designer not regenerated: 'Convert to Web Application' is not available (only Web Application projects).");
                        continue;
                    }

                    dte.ExecuteCommand(ConvertToWebApplicationCommand);
                }
                catch (Exception ex)
                {
                    AddFailures(failures, project, "Designer not regenerated: " + ex.Message);
                }
            }

            return failures;
        }

        private static void AddFailures(List<SweepFailure> failures, IEnumerable<(string Path, IVsUIHierarchy Hierarchy, uint ItemId)> items, string message)
        {
            failures.AddRange(items.Select(i => new SweepFailure(i.Path, message)));
        }
    }
}
