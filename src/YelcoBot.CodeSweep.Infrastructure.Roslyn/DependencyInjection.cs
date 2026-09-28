using Microsoft.Extensions.DependencyInjection;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Engine;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCodeSweepRoslyn(this IServiceCollection services)
        {
            services.AddSingleton<ISweepRule, RemoveUnusedLocalVariablesRule>();
            services.AddSingleton<ISweepRule, RemoveUnusedUsingsRule>();
            services.AddSingleton<ISweepRule, SortUsingsRule>();
            services.AddSingleton<ISweepRule, RemoveConsecutiveBlankLinesRule>();
            services.AddSingleton<ISweepRule, FormatDocumentRule>();

            services.AddSingleton<DocumentSweeper>();
            services.AddTransient<ICodeCleaner, RoslynCodeCleaner>();

            return services;
        }
    }
}
