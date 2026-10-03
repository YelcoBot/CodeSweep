using Microsoft.CodeAnalysis;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Engine
{
    public class DocumentSweeper
    {
        private readonly List<ISweepRule> _rules;

        public DocumentSweeper(IEnumerable<ISweepRule> rules)
        {
            _rules = rules.OrderBy(r => r.Order).ToList();
        }

        public async Task<Document> SweepAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            List<ISweepRule> activeRules = _rules.Where(r => r.IsEnabled(options)).ToList();
            Document currentDoc = document;

            foreach (ISweepRule rule in activeRules)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                currentDoc = await rule.ApplyAsync(currentDoc, options, cancellationToken);
            }

            return currentDoc;
        }
    }
}
