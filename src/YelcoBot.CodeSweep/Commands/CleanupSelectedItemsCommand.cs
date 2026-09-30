using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.UseCases;
using Task = System.Threading.Tasks.Task;

namespace YelcoBot.CodeSweep.Commands
{
    /// <summary>Clic derecho en el Explorador de soluciones → Cleanup Selected Code.</summary>
    [Command(PackageGuids.guidCodeSweepPackageCmdSetString, PackageIds.CleanupSelectedItemsCommandId)]
    internal sealed class CleanupSelectedItemsCommand : BaseCommand<CleanupSelectedItemsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            CleanupSelectedItemsUseCase? useCase = CodeSweepPackage.ServiceProvider?.GetService<CleanupSelectedItemsUseCase>();
            if (useCase != null)
            {
                await useCase.ExecuteAsync();
            }
        }
    }
}
