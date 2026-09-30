using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// Envía el comando Format Document (Ctrl+K, Ctrl+D) directo a una vista, sin necesidad de que esté activa.
    /// Antes espera a que el editor termine de cargar (servicio de lenguaje / LSP): no formatea "a medias".
    /// </summary>
    internal static class FormatDocumentCommand
    {
        /// <summary>Editor real (ventana o documento ya abierto): puede tardar en arrancar (Razor/HTML usan LSP).</summary>
        public static readonly TimeSpan RealEditorTimeout = TimeSpan.FromSeconds(5);

        /// <summary>Editor invisible: si no se habilita pronto, ese editor no formatea sin ventana (se cachea por extensión).</summary>
        public static readonly TimeSpan InvisibleEditorTimeout = TimeSpan.FromSeconds(2);

        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

        public static async Task<EditorFormatOutcome> ExecuteAsync(
            IVsTextView view,
            IVsEditorAdaptersFactoryService adapters,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ITextBuffer? buffer = adapters.GetWpfTextView(view)?.TextBuffer;
            if (buffer == null || view is not IOleCommandTarget commandTarget)
                return EditorFormatOutcome.NotHandled;

            if (!await WaitUntilReadyAsync(commandTarget, timeout, cancellationToken))
                return EditorFormatOutcome.NotHandled;

            int versionBefore = buffer.CurrentSnapshot.Version.VersionNumber;

            Guid commandGroup = VSConstants.VSStd2K;
            int hr = commandTarget.Exec(ref commandGroup, (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT, 0, IntPtr.Zero, IntPtr.Zero);
            if (ErrorHandler.Failed(hr))
                return EditorFormatOutcome.NotHandled;

            return buffer.CurrentSnapshot.Version.VersionNumber != versionBefore
                ? EditorFormatOutcome.Changed
                : EditorFormatOutcome.Unchanged;
        }

        /// <summary>
        /// Espera hasta que el comando Format Document esté soportado y habilitado en la vista.
        /// Si ya lo está, responde al instante (sin costo).
        /// </summary>
        private static async Task<bool> WaitUntilReadyAsync(IOleCommandTarget commandTarget, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (true)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                if (IsFormatDocumentEnabled(commandTarget))
                    return true;

                if (stopwatch.Elapsed >= timeout)
                    return false;

                // Ceder el hilo de UI para que el editor pueda terminar de inicializarse.
                await Task.Delay(PollInterval, cancellationToken);
            }
        }

        private static bool IsFormatDocumentEnabled(IOleCommandTarget commandTarget)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid commandGroup = VSConstants.VSStd2K;
            OLECMD[] commands = { new OLECMD { cmdID = (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT } };

            int hr = commandTarget.QueryStatus(ref commandGroup, 1, commands, IntPtr.Zero);
            const uint ready = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);

            return ErrorHandler.Succeeded(hr) && (commands[0].cmdf & ready) == ready;
        }
    }
}
