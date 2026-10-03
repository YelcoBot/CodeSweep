using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    /// <summary>
    /// Limpia archivos .sql en segundo plano: formato con <see cref="ISqlFormatter"/> + reglas universales.
    /// Si el archivo está abierto se cambia su buffer (el usuario guarda); si no, el archivo en disco.
    /// </summary>
    public interface ISqlCleaner
    {
        Task<SweepSummary> CleanAsync(
            IReadOnlyList<string> filePaths,
            SweepOptions options,
            IProgress<SweepProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
