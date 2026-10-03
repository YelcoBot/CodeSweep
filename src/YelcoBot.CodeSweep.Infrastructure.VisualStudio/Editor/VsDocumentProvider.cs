using System.IO;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    public class VsDocumentProvider : IDocumentProvider
    {
        private readonly IEditorContext _editorContext;

        public VsDocumentProvider(IEditorContext editorContext)
        {
            _editorContext = editorContext;
        }

        public async Task<IReadOnlyList<string>> GetFilePathsAsync(DocumentSelection selection, CancellationToken cancellationToken = default)
        {
            switch (selection.Type)
            {
                case DocumentSelectionType.Files:
                    return selection.FilePaths;

                case DocumentSelectionType.ActiveDocument:
                    string? activePath = await _editorContext.GetActiveDocumentPathAsync();
                    return string.IsNullOrWhiteSpace(activePath) ? Array.Empty<string>() : new[] { activePath! };

                case DocumentSelectionType.OpenDocuments:
                    return await GetOpenDocumentPathsAsync(cancellationToken);

                case DocumentSelectionType.Solution:
                    return await GetSolutionFilePathsAsync(cancellationToken);

                case DocumentSelectionType.SolutionExplorerSelection:
                    return await GetSelectedItemsFilePathsAsync(cancellationToken);

                default:
                    return Array.Empty<string>();
            }
        }

        private static async Task<IReadOnlyList<string>> GetOpenDocumentPathsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            IVsUIShell shell = await VS.GetRequiredServiceAsync<SVsUIShell, IVsUIShell>();
            List<string> paths = new List<string>();

            if (ErrorHandler.Failed(shell.GetDocumentWindowEnum(out IEnumWindowFrames frames)) || frames == null)
                return paths;

            IVsWindowFrame[] buffer = new IVsWindowFrame[1];
            while (frames.Next(1, buffer, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (ErrorHandler.Succeeded(buffer[0].GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object moniker))
                    && moniker is string path
                    && (IsUnsavedSqlQuery(path) || (Path.IsPathRooted(path) && File.Exists(path))))
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        /// <summary>
        /// SSMS: una ventana de consulta nueva (SQLQuery1.sql) no existe en disco hasta que se guarda;
        /// igual se limpia, porque el cleaner de SQL trabaja sobre su buffer.
        /// </summary>
        private static bool IsUnsavedSqlQuery(string path)
        {
            return string.Equals(Path.GetExtension(path), ".sql", StringComparison.OrdinalIgnoreCase) && !File.Exists(path);
        }

        private static async Task<IReadOnlyList<string>> GetSolutionFilePathsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            List<string> paths = new List<string>();
            foreach (Project project in await VS.Solutions.GetAllProjectsAsync())
            {
                CollectPhysicalFiles(project, paths);
            }

            return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Lo seleccionado en el Explorador de soluciones: solución, carpetas de solución, proyectos,
        /// carpetas o archivos (también selección múltiple). Se recorre de forma recursiva.
        /// </summary>
        private static async Task<IReadOnlyList<string>> GetSelectedItemsFilePathsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            List<string> paths = new List<string>();
            foreach (SolutionItem item in await VS.Solutions.GetActiveItemsAsync())
            {
                if (item.Type == SolutionItemType.PhysicalFile && !string.IsNullOrWhiteSpace(item.FullPath))
                {
                    paths.Add(item.FullPath!);
                }

                CollectPhysicalFiles(item, paths);
            }

            return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task<SolutionExplorerSelectionInfo> DescribeSolutionExplorerSelectionAsync(CancellationToken cancellationToken = default)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            List<string> selectedFiles = new List<string>();
            List<SelectedContainer> containers = new List<SelectedContainer>();

            foreach (SolutionItem item in await VS.Solutions.GetActiveItemsAsync())
            {
                if (item.Type == SolutionItemType.PhysicalFile)
                {
                    // El archivo y sus hijos (Default.aspx → .aspx.cs / .designer.cs).
                    if (!string.IsNullOrWhiteSpace(item.FullPath))
                    {
                        selectedFiles.Add(item.FullPath!);
                    }

                    CollectPhysicalFiles(item, selectedFiles);
                    continue;
                }

                List<string> paths = new List<string>();
                CollectPhysicalFiles(item, paths);
                containers.Add(new SelectedContainer(GetKindName(item.Type), item.Text, paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList()));
            }

            return new SolutionExplorerSelectionInfo(selectedFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), containers);
        }

        private static string GetKindName(SolutionItemType type)
        {
            switch (type)
            {
                case SolutionItemType.Solution: return Strings.KindSolution;
                case SolutionItemType.SolutionFolder: return Strings.KindSolutionFolder;
                case SolutionItemType.Project: return Strings.KindProject;
                case SolutionItemType.PhysicalFolder:
                case SolutionItemType.VirtualFolder: return Strings.KindFolder;
                default: return type.ToString();
            }
        }

        private static void CollectPhysicalFiles(SolutionItem item, List<string> paths)
        {
            foreach (SolutionItem? child in item.Children)
            {
                if (child == null)
                    continue;

                if (child.Type == SolutionItemType.PhysicalFile && !string.IsNullOrWhiteSpace(child.FullPath))
                {
                    paths.Add(child.FullPath!);
                }

                // Los archivos pueden tener hijos (ej. Default.aspx → Default.aspx.cs / .designer.cs).
                CollectPhysicalFiles(child, paths);
            }
        }
    }
}
