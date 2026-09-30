using System;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.Application.UseCases;
using Task = System.Threading.Tasks.Task;

namespace YelcoBot.CodeSweep.Commands
{
    [Command(PackageGuids.guidCodeSweepPackageCmdSetString, PackageIds.CleanupSolutionCommandId)]
    internal sealed class CleanupSolutionCommand : BaseCommand<CleanupSolutionCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            CleanupSolutionUseCase? useCase = CodeSweepPackage.ServiceProvider?.GetService<CleanupSolutionUseCase>();
            if (useCase != null)
            {
                await useCase.ExecuteAsync();
            }
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Text = Strings.CommandCleanupAllCode;
        }
    }
}
