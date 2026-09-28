using System;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.UseCases;
using Task = System.Threading.Tasks.Task;

namespace YelcoBot.CodeSweep.Vsix.Commands
{
    [Command(PackageGuids.guidCodeSweepPackageCmdSetString, PackageIds.ToggleCleanupOnSaveCommandId)]
    internal sealed class ToggleCleanupOnSaveCommand : BaseCommand<ToggleCleanupOnSaveCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            ToggleCleanupOnSaveUseCase? useCase = CodeSweepPackage.ServiceProvider?.GetService<ToggleCleanupOnSaveUseCase>();
            if (useCase != null)
            {
                bool newState = await useCase.ExecuteAsync();
                Command.Checked = newState;
            }
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            ISettingsStore? settingsStore = CodeSweepPackage.ServiceProvider?.GetService<ISettingsStore>();
            if (settingsStore != null)
            {
                YelcoBot.CodeSweep.Domain.Options.SweepOptions options = ThreadHelper.JoinableTaskFactory.Run(async () => await settingsStore.GetOptionsAsync());
                Command.Checked = options.CleanupOnSave;
            }
        }
    }
}
