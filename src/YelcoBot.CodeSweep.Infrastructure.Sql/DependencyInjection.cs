using Microsoft.Extensions.DependencyInjection;
using YelcoBot.CodeSweep.Application.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Sql
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCodeSweepSql(this IServiceCollection services)
        {
            services.AddSingleton<ISqlFormatter, IsolatedSqlFormatter>();
            return services;
        }
    }
}
