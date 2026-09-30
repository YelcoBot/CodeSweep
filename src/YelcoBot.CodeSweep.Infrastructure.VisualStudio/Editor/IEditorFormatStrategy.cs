using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Routing;

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
        Task<EditorFormatOutcome> TryFormatAsync(string filePath, SweepOptions options, CancellationToken cancellationToken);
    }

    internal static class EditorFormatRules
    {
        /// <summary>
        /// Web Forms con designer y "Regenerate Web Forms designer" activo: se formatean en el editor real
        /// (ventana), porque al guardar ahí VS regenera el .designer.cs. Todo lo demás, solo en el editor invisible.
        /// </summary>
        public static bool UsesRealEditor(string filePath, SweepOptions options)
        {
            return options.RegenerateWebFormsDesigner
                && FileTypeGroups.WebFormsWithDesigner.Contains(Path.GetExtension(filePath));
        }
    }
}
