using System;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.UseCases;
using Task = System.Threading.Tasks.Task;

namespace YelcoBot.CodeSweep.Vsix.Commands
{
    [Command(PackageGuids.guidCodeSweepPackageCmdSetString, PackageIds.CleanupActiveDocumentCommandId)]
    internal sealed class CleanupActiveDocumentCommand : BaseCommand<CleanupActiveDocumentCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            CleanupActiveDocumentUseCase? useCase = CodeSweepPackage.ServiceProvider?.GetService<CleanupActiveDocumentUseCase>();
            if (useCase != null)
            {
                await useCase.ExecuteAsync();
            }
        }
    }
}
