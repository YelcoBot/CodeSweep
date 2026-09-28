using Community.VisualStudio.Toolkit;
using YelcoBot.CodeSweep.Application.Abstractions;

using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    public class VsEditorContext : IEditorContext
    {
        public async Task<string?> GetActiveDocumentPathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            DocumentView? docView = await VS.Documents.GetActiveDocumentViewAsync();
            return docView?.FilePath;
        }
    }
}
