using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Elimina variables locales sin usar: CS0168 (declarada y nunca usada) y CS0219 (asignada con un valor que nunca se lee).
    /// Solo elimina declaraciones de una sola variable y sin otras referencias, para no romper la compilación.
    /// </summary>
    public class RemoveUnusedLocalVariablesRule : ISweepRule
    {
        public string Id => "CS_REMOVE_UNUSED_LOCALS";
        public int Order => 10;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveUnusedLocalVariables;

        public async Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default)
        {
            if (document.Project.Language != LanguageNames.CSharp)
                return document;

            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);

            if (semanticModel == null || syntaxRoot == null)
                return document;

            ImmutableArray<Diagnostic> diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);

            // Con errores de compilación el análisis no es confiable: no tocar nada.
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return document;

            HashSet<LocalDeclarationStatementSyntax> nodesToRemove = new HashSet<LocalDeclarationStatementSyntax>();
            foreach (Diagnostic diagnostic in diagnostics.Where(d => d.Id is "CS0168" or "CS0219"))
            {
                SyntaxNode node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                LocalDeclarationStatementSyntax? localDeclaration = node.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();

                if (localDeclaration == null || localDeclaration.Declaration.Variables.Count != 1)
                    continue;

                if (IsReferencedElsewhere(localDeclaration, semanticModel, cancellationToken))
                    continue;

                nodesToRemove.Add(localDeclaration);
            }

            if (nodesToRemove.Count == 0)
                return document;

            SyntaxNode? newRoot = syntaxRoot.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            return newRoot == null ? document : document.WithSyntaxRoot(newRoot);
        }

        /// <summary>CS0219 también se reporta con reasignaciones posteriores (x = 5;): esas referencias quedarían huérfanas.</summary>
        private static bool IsReferencedElsewhere(LocalDeclarationStatementSyntax localDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            VariableDeclaratorSyntax declarator = localDeclaration.Declaration.Variables[0];
            ISymbol? symbol = semanticModel.GetDeclaredSymbol(declarator, cancellationToken);
            SyntaxNode? scope = localDeclaration.Parent;

            if (symbol == null || scope == null)
                return true;

            string name = declarator.Identifier.ValueText;
            return scope.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(identifier => identifier.Identifier.ValueText == name)
                .Any(identifier => SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol, symbol));
        }
    }
}
