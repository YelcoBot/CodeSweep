using System.Threading;
using System.Threading.Tasks;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    public enum EditorFormatOutcome
    {
        /// <summary>La estrategia no aplica o no pudo formatear: se intenta con la siguiente.</summary>
        NotHandled,
        Unchanged,
        Changed
    }

    public interface IEditorFormatStrategy
    {
        int Order { get; }

        /// <summary>Se ejecuta en el hilo de UI.</summary>
        Task<EditorFormatOutcome> TryFormatAsync(string filePath, CancellationToken cancellationToken);
    }
}
