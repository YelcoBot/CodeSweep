using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    public class FormatDocumentRule : ISweepRule
    {
        public string Id => "CS_FORMAT_DOCUMENT";
        public int Order => 50;

        public bool IsEnabled(SweepOptions options) => options.EnableFormatDocument;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            return await Formatter.FormatAsync(document, cancellationToken: cancellationToken);
        }
    }
}
