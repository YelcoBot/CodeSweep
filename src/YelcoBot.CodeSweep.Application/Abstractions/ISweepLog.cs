using System.Threading.Tasks;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    /// <summary>Registro de actividad de CodeSweep (en VS: ventana Output → CodeSweep).</summary>
    public interface ISweepLog
    {
        Task WriteLineAsync(string message);
    }
}
