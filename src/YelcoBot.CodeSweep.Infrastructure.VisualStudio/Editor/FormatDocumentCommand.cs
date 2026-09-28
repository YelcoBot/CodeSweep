using System;
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
    /// </summary>
    internal static class FormatDocumentCommand
    {
        public static EditorFormatOutcome Execute(IVsTextView view, IVsEditorAdaptersFactoryService adapters)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ITextBuffer? buffer = adapters.GetWpfTextView(view)?.TextBuffer;
            if (buffer == null || view is not IOleCommandTarget commandTarget)
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
    }
}
