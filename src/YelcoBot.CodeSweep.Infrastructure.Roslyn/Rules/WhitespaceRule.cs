using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Services.Whitespace;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Text;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Reglas universales (1.1, 1.2, 1.3, 2.1, 2.2) en C# y VB, después de formatear.
    /// Nunca modifica strings multilínea, raw strings, literales XML de VB ni código deshabilitado (#if false / #If False).
    /// </summary>
    public class WhitespaceRule : ISweepRule
    {
        public string Id => "WHITESPACE";
        public int Order => 60;

        public bool IsEnabled(SweepOptions options) => WhitespaceCleaner.IsAnyEnabled(options);

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot == null)
                return document;

            SourceText text = await document.GetTextAsync(cancellationToken);
            List<TextLineInfo> lines = SyntaxTextGuard.GetLines(text);

            IReadOnlyList<Domain.Services.Whitespace.TextEdit> edits = WhitespaceCleaner.FindEdits(lines, new SyntaxTextGuard(syntaxRoot, lines), options);
            if (edits.Count == 0)
                return document;

            return document.WithText(text.WithChanges(edits.Select(e => new TextChange(new TextSpan(e.Start, e.Length), e.NewText))));
        }
    }
}
