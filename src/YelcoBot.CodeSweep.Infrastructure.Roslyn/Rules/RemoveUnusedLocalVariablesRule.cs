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
    public class RemoveUnusedLocalVariablesRule : ISweepRule
    {
        public string Id => "CS_REMOVE_UNUSED_LOCALS";
        public int Order => 10;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveUnusedLocalVariables;

        public async Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default)
        {
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);

            if (semanticModel == null || syntaxRoot == null)
                return document;

            IEnumerable<Diagnostic> diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
            List<Diagnostic> unusedLocalDiagnostics = diagnostics
                .Where(d => d.Id is "CS0168" or "CS0219")
                .ToList();

            if (!unusedLocalDiagnostics.Any())
                return document;

            HashSet<SyntaxNode> nodesToRemove = new HashSet<SyntaxNode>();
            foreach (Diagnostic diag in unusedLocalDiagnostics)
            {
                SyntaxNode node = syntaxRoot.FindNode(diag.Location.SourceSpan);
                LocalDeclarationStatementSyntax? localDeclaration = node.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                if (localDeclaration != null)
                {
                    if (localDeclaration.Declaration.Variables.Count == 1)
                    {
                        nodesToRemove.Add(localDeclaration);
                    }
                }
            }

            if (nodesToRemove.Any())
            {
                SyntaxNode? newRoot = syntaxRoot.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                if (newRoot != null)
                {
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            return document;
        }
    }
}
