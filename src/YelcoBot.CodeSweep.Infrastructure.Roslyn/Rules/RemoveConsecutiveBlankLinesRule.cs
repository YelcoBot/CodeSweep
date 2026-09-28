using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    public class RemoveConsecutiveBlankLinesRule : ISweepRule
    {
        private static readonly Regex MultipleBlankLinesRegex = new Regex(@"(\r?\n){3,}", RegexOptions.Compiled);

        public string Id => "CS_REMOVE_BLANK_LINES";
        public int Order => 40;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveConsecutiveBlankLines;

        public async Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default)
        {
            SourceText text = await document.GetTextAsync(cancellationToken);
            string sourceString = text.ToString();

            string cleaned = MultipleBlankLinesRegex.Replace(sourceString, "$1$1");
            if (cleaned != sourceString)
            {
                return document.WithText(SourceText.From(cleaned, text.Encoding));
            }

            return document;
        }
    }
}
