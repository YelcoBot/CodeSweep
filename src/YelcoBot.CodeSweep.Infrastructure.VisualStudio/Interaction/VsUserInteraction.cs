using System.Text;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Localization;
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

        public async Task<UserChoice> AskYesNoCancelAsync(string title, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VSConstants.MessageBoxResult result = await VS.MessageBox.ShowAsync(
                title,
                message,
                OLEMSGICON.OLEMSGICON_QUERY,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_THIRD);

            switch (result)
            {
                case VSConstants.MessageBoxResult.IDYES: return UserChoice.Yes;
                case VSConstants.MessageBoxResult.IDNO: return UserChoice.No;
                default: return UserChoice.Cancel;
            }
        }

        public async Task ShowSummaryAsync(SweepSummary summary)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (summary.IsCancelled)
            {
                await VS.StatusBar.ShowMessageAsync(Strings.SummaryCancelled);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Strings.Format(Strings.SummaryProcessed, summary.ProcessedFilesCount));
            sb.AppendLine(Strings.Format(Strings.SummaryChanged, summary.ChangedFilesCount));
            sb.AppendLine(Strings.Format(Strings.SummaryDuration, summary.Duration.TotalSeconds));

            if (summary.Failures.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(Strings.Format(Strings.SummaryFailures, summary.Failures.Count));
                foreach (SweepFailure fail in summary.Failures)
                {
                    sb.AppendLine($" - {fail.FilePath}: {fail.ErrorMessage}");
                }
            }

            await VS.MessageBox.ShowAsync(Strings.SummaryTitle, sb.ToString());
            await VS.StatusBar.ShowMessageAsync(Strings.Format(Strings.SummaryStatusBar, summary.ChangedFilesCount));
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
