using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.UseCases;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Events
{
    public class SaveEventListener
    {
        private readonly CleanupOnSaveUseCase _cleanupOnSaveUseCase;

        public SaveEventListener(CleanupOnSaveUseCase cleanupOnSaveUseCase)
        {
            _cleanupOnSaveUseCase = cleanupOnSaveUseCase;
        }

        public void Register()
        {
            VS.Events.DocumentEvents.Saved += OnDocumentSaved;
        }

        private void OnDocumentSaved(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !filePath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await _cleanupOnSaveUseCase.ExecuteAsync(filePath);
            });
        }
    }
}
