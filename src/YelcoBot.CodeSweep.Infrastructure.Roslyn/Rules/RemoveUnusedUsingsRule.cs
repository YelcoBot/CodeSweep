using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    public class RemoveUnusedUsingsRule : ISweepRule
    {
        public string Id => "CS_REMOVE_UNUSED_USINGS";
        public int Order => 20;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveUnusedUsings;

        public async Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default)
        {
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);

            if (semanticModel == null || syntaxRoot == null)
                return document;

            IEnumerable<Diagnostic> diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
            List<Diagnostic> unusedUsingDiagnostics = diagnostics
                .Where(d => d.Id == "CS8019")
                .ToList();

            if (!unusedUsingDiagnostics.Any())
                return document;

            HashSet<UsingDirectiveSyntax> usingsToRemove = new HashSet<UsingDirectiveSyntax>();
            foreach (Diagnostic diag in unusedUsingDiagnostics)
            {
                SyntaxNode node = syntaxRoot.FindNode(diag.Location.SourceSpan);
                UsingDirectiveSyntax? usingDirective = node.AncestorsAndSelf().OfType<UsingDirectiveSyntax>().FirstOrDefault();
                if (usingDirective != null)
                {
                    usingsToRemove.Add(usingDirective);
                }
            }

            if (usingsToRemove.Any())
            {
                SyntaxNode? newRoot = syntaxRoot.RemoveNodes(usingsToRemove, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                if (newRoot != null)
                {
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            return document;
        }
    }
}
