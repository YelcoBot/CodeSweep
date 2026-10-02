using System;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// Estrategia 1: el documento ya está abierto → formatear su vista. No se guarda (es el documento del usuario).
    /// </summary>
    public class OpenDocumentFormatStrategy : IEditorFormatStrategy
    {
        public int Order => 1;

        public async Task<EditorFormatOutcome> TryFormatAsync(string filePath, SweepOptions options, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (!VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, filePath, Guid.Empty, out _, out _, out IVsWindowFrame? frame)
                || frame == null)
            {
                return EditorFormatOutcome.NotHandled;
            }

            IVsTextView? view = VsShellUtilities.GetTextView(frame);
            if (view == null)
                return EditorFormatOutcome.NotHandled;

            IVsEditorAdaptersFactoryService adapters = await VS.GetMefServiceAsync<IVsEditorAdaptersFactoryService>();
            return await FormatDocumentCommand.ExecuteAsync(view, adapters, filePath, options.EnableRemoveConsecutiveBlankLines, FormatDocumentCommand.RealEditorTimeout, cancellationToken);
        }
    }
}
