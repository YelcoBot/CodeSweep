using System;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// Estrategia 3: abrir en el editor real → formatear → guardar → cerrar.
    /// Automático SOLO para Web Forms con "Regenerate Web Forms designer" activo (al guardar en el editor real VS regenera el designer).
    /// Para el resto, solo si el usuario lo acepta cuando el editor invisible no pudo (ver VsEditorFormatter).
    /// </summary>
    public class WindowFormatStrategy : IEditorFormatStrategy
    {
        public int Order => 3;

        public Task<EditorFormatOutcome> TryFormatAsync(string filePath, SweepOptions options, CancellationToken cancellationToken)
        {
            return EditorFormatRules.UsesRealEditor(filePath, options)
                ? FormatInEditorAsync(filePath, options, cancellationToken)
                : Task.FromResult(EditorFormatOutcome.NotHandled);
        }

        /// <summary>Formatea en el editor real sin importar el tipo de archivo (el usuario lo pidió).</summary>
        public async Task<EditorFormatOutcome> FormatInEditorAsync(string filePath, SweepOptions options, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Nunca cerrar un documento que el usuario ya tenía abierto.
            if (VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, filePath, Guid.Empty, out _, out _, out _))
                return EditorFormatOutcome.NotHandled;

            VsShellUtilities.OpenDocument(
                ServiceProvider.GlobalProvider,
                filePath,
                VSConstants.LOGVIEWID_TextView,
                out _,
                out _,
                out IVsWindowFrame? frame);

            if (frame == null)
                return EditorFormatOutcome.NotHandled;

            try
            {
                // La vista de texto se crea al mostrar el frame.
                frame.Show();

                IVsTextView? view = VsShellUtilities.GetTextView(frame);
                if (view == null)
                    return EditorFormatOutcome.NotHandled;

                IVsEditorAdaptersFactoryService adapters = await VS.GetMefServiceAsync<IVsEditorAdaptersFactoryService>();
                return await FormatDocumentCommand.ExecuteAsync(view, adapters, filePath, options.EnableRemoveConsecutiveBlankLines, FormatDocumentCommand.RealEditorTimeout, cancellationToken);
            }
            finally
            {
                frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_SaveIfDirty);
            }
        }
    }
}
