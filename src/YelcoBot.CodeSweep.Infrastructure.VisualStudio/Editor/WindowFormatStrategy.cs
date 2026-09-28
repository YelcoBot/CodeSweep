using System;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// Estrategia 3 (respaldo): abrir en ventana → formatear → guardar → cerrar. Lenta pero siempre funciona.
    /// </summary>
    public class WindowFormatStrategy : IEditorFormatStrategy
    {
        public int Order => 3;

        public async Task<EditorFormatOutcome> TryFormatAsync(string filePath, CancellationToken cancellationToken)
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
                return FormatDocumentCommand.Execute(view, adapters);
            }
            finally
            {
                frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_SaveIfDirty);
            }
        }
    }
}
