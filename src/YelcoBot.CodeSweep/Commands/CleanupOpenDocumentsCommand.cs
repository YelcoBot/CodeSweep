using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.UseCases;
using Task = System.Threading.Tasks.Task;

namespace YelcoBot.CodeSweep.Commands
{
    [Command(PackageGuids.guidCodeSweepPackageCmdSetString, PackageIds.CleanupOpenDocumentsCommandId)]
    internal sealed class CleanupOpenDocumentsCommand : BaseCommand<CleanupOpenDocumentsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            CleanupOpenDocumentsUseCase? useCase = CodeSweepPackage.ServiceProvider?.GetService<CleanupOpenDocumentsUseCase>();
            if (useCase != null)
            {
                await useCase.ExecuteAsync();
            }
        }
    }
}
