using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using YelcoBot.CodeSweep.Application.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Interaction
{
    /// <summary>Escribe en la ventana Output, panel "CodeSweep".</summary>
    public class VsOutputLog : ISweepLog
    {
        private OutputWindowPane? _pane;

        public async Task WriteLineAsync(string message)
        {
            try
            {
                _pane ??= await VS.Windows.CreateOutputWindowPaneAsync("CodeSweep", lazyCreate: true);
                await _pane.WriteLineAsync($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
            catch (Exception ex)
            {
                // El log nunca debe romper un cleanup.
                await ex.LogAsync();
            }
        }
    }
}
