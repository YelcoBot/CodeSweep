using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions
{
    public interface ISweepRule
    {
        string Id { get; }
        int Order { get; }
        bool IsEnabled(SweepOptions options);
        Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default);
    }
}
