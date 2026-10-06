using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Workspace
{
    /// <summary>
    /// Open Folder mode (File → Open → Folder): there is no solution or project, so files are read
    /// from disk and the Solution Explorer selection is resolved by path.
    /// </summary>
    internal static class OpenFolderWorkspace
    {
        private static readonly HashSet<string> ExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin",
            "obj",
            "node_modules",
        };

        /// <summary>Returns the open folder, or null when a solution (or nothing) is open.</summary>
        public static string? GetRootFolder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!(Package.GetGlobalService(typeof(SVsSolution)) is IVsSolution solution))
                return null;

            if (ErrorHandler.Failed(solution.GetProperty((int)__VSPROPID7.VSPROPID_IsInOpenFolderMode, out object isOpenFolder))
                || !(isOpenFolder is bool openFolder)
                || !openFolder)
            {
                return null;
            }

            if (ErrorHandler.Failed(solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out object directory))
                || !(directory is string root)
                || !Directory.Exists(root))
            {
                return null;
            }

            return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Returns every file under the folder, recursively. Skips bin, obj, node_modules
        /// and folders whose name starts with a dot.
        /// </summary>
        public static List<string> GetFiles(string folder)
        {
            List<string> files = new List<string>();
            CollectFiles(folder, files);
            return files;
        }

        /// <summary>
        /// Returns the files and folders selected in Solution Explorer.
        /// The root node exposes no path, so it resolves to <paramref name="rootFolder"/>.
        /// </summary>
        public static List<string> GetSelectedPaths(string rootFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<string> paths = new List<string>();

            if (!(Package.GetGlobalService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection monitorSelection)
                || ErrorHandler.Failed(monitorSelection.GetCurrentSelection(out IntPtr hierarchyPointer, out uint itemId, out IVsMultiItemSelect multiSelect, out IntPtr containerPointer)))
            {
                return paths;
            }

            try
            {
                IVsHierarchy? hierarchy = hierarchyPointer == IntPtr.Zero ? null : Marshal.GetObjectForIUnknown(hierarchyPointer) as IVsHierarchy;

                foreach (VSITEMSELECTION item in GetSelectedItems(hierarchy, itemId, multiSelect))
                {
                    AddPath(item.pHier ?? hierarchy, item.itemid, rootFolder, paths);
                }
            }
            finally
            {
                Release(hierarchyPointer);
                Release(containerPointer);
            }

            return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static VSITEMSELECTION[] GetSelectedItems(IVsHierarchy? hierarchy, uint itemId, IVsMultiItemSelect? multiSelect)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (itemId != VSConstants.VSITEMID_SELECTION || multiSelect == null)
                return new[] { new VSITEMSELECTION { pHier = hierarchy, itemid = itemId } };

            if (ErrorHandler.Failed(multiSelect.GetSelectionInfo(out uint count, out _)) || count == 0)
                return Array.Empty<VSITEMSELECTION>();

            VSITEMSELECTION[] items = new VSITEMSELECTION[count];
            return ErrorHandler.Succeeded(multiSelect.GetSelectedItems(0, count, items)) ? items : Array.Empty<VSITEMSELECTION>();
        }

        private static void Release(IntPtr pointer)
        {
            if (pointer != IntPtr.Zero)
            {
                Marshal.Release(pointer);
            }
        }

        private static void AddPath(IVsHierarchy? hierarchy, uint itemId, string rootFolder, List<string> paths)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchy == null || ErrorHandler.Failed(hierarchy.GetCanonicalName(itemId, out string canonicalName)))
                return;

            bool isRootNode = string.IsNullOrEmpty(canonicalName);
            if (isRootNode)
            {
                paths.Add(rootFolder);
            }
            else if (IsExistingPath(canonicalName))
            {
                paths.Add(canonicalName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
        }

        private static bool IsExistingPath(string? path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path) && (File.Exists(path) || Directory.Exists(path));
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static void CollectFiles(string folder, List<string> files)
        {
            files.AddRange(ReadEntries(() => Directory.GetFiles(folder)));

            foreach (string subfolder in ReadEntries(() => Directory.GetDirectories(folder)))
            {
                string name = Path.GetFileName(subfolder);
                if (name.StartsWith(".") || ExcludedFolders.Contains(name))
                    continue;

                CollectFiles(subfolder, files);
            }
        }

        /// <summary>Returns the entries, or none when the folder cannot be read.</summary>
        private static string[] ReadEntries(Func<string[]> read)
        {
            try
            {
                return read();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                return Array.Empty<string>();
            }
        }
    }
}
