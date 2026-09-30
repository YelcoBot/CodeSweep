using System.Collections.Generic;

namespace YelcoBot.CodeSweep.Domain.Selection
{
    /// <summary>Contenedor seleccionado en el Explorador de soluciones (solución, proyecto, carpeta).</summary>
    public sealed class SelectedContainer
    {
        public string Kind { get; }
        public string Name { get; }
        public IReadOnlyList<string> FilePaths { get; }

        public SelectedContainer(string kind, string name, IReadOnlyList<string> filePaths)
        {
            Kind = kind;
            Name = name;
            FilePaths = filePaths;
        }
    }

    /// <summary>Qué hay en la selección del Explorador de soluciones: archivos sueltos y contenedores.</summary>
    public sealed class SolutionExplorerSelectionInfo
    {
        public int SelectedFileCount { get; }
        public IReadOnlyList<SelectedContainer> Containers { get; }

        /// <summary>Hay al menos un contenedor (solución, carpeta de solución, proyecto o carpeta): se pide confirmación.</summary>
        public bool HasContainers => Containers.Count > 0;

        public SolutionExplorerSelectionInfo(int selectedFileCount, IReadOnlyList<SelectedContainer> containers)
        {
            SelectedFileCount = selectedFileCount;
            Containers = containers;
        }
    }
}
