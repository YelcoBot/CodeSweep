using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Interaction
{
    public class VsUserInteraction : IUserInteraction
    {
        public async Task<bool> ConfirmAsync(string title, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return await VS.MessageBox.ShowConfirmAsync(title, message);
        }

        public async Task ShowSummaryAsync(SweepSummary summary)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (summary.IsCancelled)
            {
                await VS.StatusBar.ShowMessageAsync("CodeSweep operation was cancelled.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Processed: {summary.ProcessedFilesCount} file(s)");
            sb.AppendLine($"Changed: {summary.ChangedFilesCount} file(s)");
            sb.AppendLine($"Duration: {summary.Duration.TotalSeconds:F2} seconds");

            if (summary.Failures.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Failures: {summary.Failures.Count}");
                foreach (SweepFailure fail in summary.Failures)
                {
                    sb.AppendLine($" - {fail.FilePath}: {fail.ErrorMessage}");
                }
            }

            await VS.MessageBox.ShowAsync("CodeSweep Summary", sb.ToString());
            await VS.StatusBar.ShowMessageAsync($"CodeSweep finished: {summary.ChangedFilesCount} files cleaned.");
        }

        public async Task RunWithProgressAsync(string title, Func<IProgress<SweepProgress>, CancellationToken, Task> action)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Progress<SweepProgress> progress = new Progress<SweepProgress>(p =>
            {
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await VS.StatusBar.ShowProgressAsync($"{title} ({p.Current}/{p.Total})", p.Current, p.Total);
                });
            });

            try
            {
                await action(progress, CancellationToken.None);
            }
            finally
            {
                await VS.StatusBar.ShowProgressAsync(title, 0, 0);
                await VS.StatusBar.ClearAsync();
            }
        }
    }
}
