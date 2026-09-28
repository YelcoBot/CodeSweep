using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions
{
    public interface IWorkspaceAccessor
    {
        Solution? CurrentSolution { get; }
        IReadOnlyList<DocumentId> GetOpenDocumentIds();
        Task<bool> TryApplyChangesAsync(Solution newSolution, CancellationToken cancellationToken = default);
    }
}
