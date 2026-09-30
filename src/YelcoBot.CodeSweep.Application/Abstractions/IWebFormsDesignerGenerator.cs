using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    /// <summary>Regenera los .designer.cs de archivos Web Forms (.aspx, .ascx, .master).</summary>
    public interface IWebFormsDesignerGenerator
    {
        Task<IReadOnlyList<SweepFailure>> RegenerateAsync(IReadOnlyList<string> filePaths, CancellationToken cancellationToken = default);
    }
}
