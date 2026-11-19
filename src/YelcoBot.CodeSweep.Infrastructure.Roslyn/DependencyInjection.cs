using Microsoft.Extensions.DependencyInjection;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Engine;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.UnusedMethods;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCodeSweepRoslyn(this IServiceCollection services)
        {
            services.AddSingleton<MarkupIdentifierIndex>();
            services.AddSingleton<SolutionNameIndex>();
            services.AddSingleton<ISweepRule, RemoveUnusedMethodsRule>();
            services.AddSingleton<ISweepRule, RemoveUnusedLocalVariablesRule>();
            services.AddSingleton<ISweepRule, RemoveUnusedUsingsRule>();
            services.AddSingleton<ISweepRule, SortUsingsRule>();
            services.AddSingleton<ISweepRule, ReorganizeMembersRule>();
            services.AddSingleton<ISweepRule, UniformAccessorsRule>();
            services.AddSingleton<ISweepRule, RegionsRule>();
            services.AddSingleton<ISweepRule, BlankLineLayoutRule>();
            services.AddSingleton<ISweepRule, CommentsRule>();
            services.AddSingleton<ISweepRule, WhitespaceRule>();
            services.AddSingleton<ISweepRule, FormatDocumentRule>();

            services.AddSingleton<DocumentSweeper>();
            services.AddTransient<ICodeCleaner, RoslynCodeCleaner>();

            return services;
        }
    }
}
