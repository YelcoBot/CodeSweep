using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.Infrastructure.Roslyn;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Events;

namespace YelcoBot.CodeSweep
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideBindingPath] // VS busca las DLLs de la extensión (Application, Roslyn, DI…) en su carpeta de instalación.
    [InstalledProductRegistration("CodeSweep", "Clean Architecture + Roslyn automated code cleanup", "1.0.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Tools → Options clásico (VS 2022). En VS 2026 lo reemplaza la página moderna (legacyOptionPageId en el registration.json).
    [ProvideOptionPage(typeof(Options.ClassicOptionsPage), "CodeSweep", "General", 0, 0, true)]
    // Cargar al abrir una solución: textos del menú en el idioma de VS y "cleanup on save" activo sin usar antes un comando.
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuids.guidCodeSweepPackageString)]
    public sealed class CodeSweepPackage : ToolkitPackage
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Idioma de la interfaz de VS (en el hilo principal): inglés o español.
            Strings.Culture = CultureInfo.CurrentUICulture;

            ServiceCollection services = new ServiceCollection();
            services.AddCodeSweepApplication();
            services.AddCodeSweepRoslyn();
            services.AddCodeSweepVisualStudio();

            ServiceProvider = services.BuildServiceProvider();

            // Register commands
            await this.RegisterCommandsAsync();

            // Register save listener
            SaveEventListener? saveListener = ServiceProvider.GetService<SaveEventListener>();
            if (saveListener != null)
            {
                await saveListener.RegisterAsync();
            }
        }
    }
}
