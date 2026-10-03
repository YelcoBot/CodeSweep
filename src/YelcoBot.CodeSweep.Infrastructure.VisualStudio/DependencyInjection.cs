using Microsoft.Extensions.DependencyInjection;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Events;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Interaction;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Sql;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Workspace;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCodeSweepVisualStudio(this IServiceCollection services)
        {
            services.AddSingleton<IWorkspaceAccessor, VsWorkspaceAccessor>();
            services.AddSingleton<IEditorContext, VsEditorContext>();
            services.AddSingleton<IDocumentProvider, VsDocumentProvider>();

            services.AddSingleton<IEditorFormatStrategy, OpenDocumentFormatStrategy>();
            services.AddSingleton<IEditorFormatStrategy, InvisibleEditorFormatStrategy>();
            services.AddSingleton<WindowFormatStrategy>();
            services.AddSingleton<IEditorFormatStrategy>(sp => sp.GetRequiredService<WindowFormatStrategy>());
            services.AddSingleton<IEditorFormatter, VsEditorFormatter>();
            services.AddSingleton<IWebFormsDesignerGenerator, VsWebFormsDesignerGenerator>();
            services.AddSingleton<ISqlCleaner, VsSqlCleaner>();

            services.AddSingleton<IUserInteraction, VsUserInteraction>();
            services.AddSingleton<ISweepLog, VsOutputLog>();
            services.AddSingleton<ISettingsStore, VsSettingsStore>();
            services.AddSingleton<SaveEventListener>();

            return services;
        }
    }
}
