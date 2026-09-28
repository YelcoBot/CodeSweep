using Microsoft.Extensions.DependencyInjection;
using YelcoBot.CodeSweep.Domain.Services;

namespace YelcoBot.CodeSweep.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCodeSweepApplication(this IServiceCollection services)
        {
            services.AddSingleton<GeneratedCodeDetector>();

            services.AddTransient<UseCases.CleanupActiveDocumentUseCase>();
            services.AddTransient<UseCases.CleanupOpenDocumentsUseCase>();
            services.AddTransient<UseCases.CleanupSolutionUseCase>();
            services.AddTransient<UseCases.CleanupOnSaveUseCase>();
            services.AddTransient<UseCases.ToggleCleanupOnSaveUseCase>();

            return services;
        }
    }
}
