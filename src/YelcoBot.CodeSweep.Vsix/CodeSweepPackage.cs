using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application;
using YelcoBot.CodeSweep.Infrastructure.Roslyn;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Events;

namespace YelcoBot.CodeSweep.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("CodeSweep", "Clean Architecture + Roslyn automated code cleanup", "1.0.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Options.OptionsProvider.GeneralOptionsPage), "CodeSweep", "General", 0, 0, true)]
    [Guid(PackageGuids.guidCodeSweepPackageString)]
    public sealed class CodeSweepPackage : ToolkitPackage
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ServiceCollection services = new ServiceCollection();
            services.AddCodeSweepApplication();
            services.AddCodeSweepRoslyn();
            services.AddCodeSweepVisualStudio();

            ServiceProvider = services.BuildServiceProvider();

            // Register commands
            await this.RegisterCommandsAsync();

            // Register save listener
            SaveEventListener? saveListener = ServiceProvider.GetService<SaveEventListener>();
            saveListener?.Register();
        }
    }
}
