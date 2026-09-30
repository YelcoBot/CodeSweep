using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    public interface IDocumentProvider
    {
        Task<IReadOnlyList<string>> GetFilePathsAsync(DocumentSelection selection, CancellationToken cancellationToken = default);

        /// <summary>Describe la selección actual del Explorador de soluciones (archivos sueltos vs. contenedores).</summary>
        Task<SolutionExplorerSelectionInfo> DescribeSolutionExplorerSelectionAsync(CancellationToken cancellationToken = default);
    }
}
