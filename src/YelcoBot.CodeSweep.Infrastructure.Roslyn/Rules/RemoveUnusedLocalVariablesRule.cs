using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using VB = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Elimina variables locales sin usar.
    /// C#: CS0168 (declarada y nunca usada) y CS0219 (asignada con un valor que nunca se lee).
    /// VB: BC42024 (variable local sin usar) y BC42099 (constante local sin usar).
    /// Solo elimina declaraciones de una sola variable, sin otras referencias y sin inicializador con efectos
    /// (llamadas, New), para no romper la compilación ni cambiar el comportamiento.
    /// </summary>
    public class RemoveUnusedLocalVariablesRule : ISweepRule
    {
        public string Id => "REMOVE_UNUSED_LOCALS";

        public int Order => 10;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveUnusedLocalVariables;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            if (document.Project.Language is not (LanguageNames.CSharp or LanguageNames.VisualBasic))
                return document;

            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);

            if (semanticModel == null || syntaxRoot == null)
                return document;

            ImmutableArray<Diagnostic> diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);

            // Con errores de compilación el análisis no es confiable: no tocar nada.
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return document;

            HashSet<SyntaxNode> nodesToRemove = new HashSet<SyntaxNode>();
            foreach (Diagnostic diagnostic in diagnostics.Where(d => d.Id is "CS0168" or "CS0219" or "BC42024" or "BC42099"))
            {
                SyntaxNode node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                SyntaxNode? declaration = document.Project.Language == LanguageNames.CSharp
                    ? FindCSharpDeclaration(node)
                    : FindVisualBasicDeclaration(node, semanticModel, cancellationToken);

                if (declaration != null)
                {
                    nodesToRemove.Add(declaration);
                }
            }

            if (nodesToRemove.Count == 0)
                return document;

            SyntaxNode? newRoot = syntaxRoot.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            return newRoot == null ? document : document.WithSyntaxRoot(newRoot);

            SyntaxNode? FindCSharpDeclaration(SyntaxNode node)
            {
                LocalDeclarationStatementSyntax? localDeclaration = node.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                if (localDeclaration == null || localDeclaration.Declaration.Variables.Count != 1)
                    return null;

                VariableDeclaratorSyntax declarator = localDeclaration.Declaration.Variables[0];
                return IsReferencedElsewhere<IdentifierNameSyntax>(declarator, declarator.Identifier.ValueText, localDeclaration.Parent, semanticModel, cancellationToken)
                    ? null
                    : localDeclaration;
            }
        }

        private static SyntaxNode? FindVisualBasicDeclaration(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            VB.LocalDeclarationStatementSyntax? localDeclaration = node.AncestorsAndSelf().OfType<VB.LocalDeclarationStatementSyntax>().FirstOrDefault();
            if (localDeclaration == null || localDeclaration.Declarators.Count != 1)
                return null;

            VB.VariableDeclaratorSyntax declarator = localDeclaration.Declarators[0];
            if (declarator.Names.Count != 1 || declarator.AsClause is VB.AsNewClauseSyntax)
                return null;

            // Dim x = Calcular(): quitarla también quitaría la llamada. Solo se quitan valores constantes.
            if (declarator.Initializer != null && !semanticModel.GetConstantValue(declarator.Initializer.Value, cancellationToken).HasValue)
                return null;

            VB.ModifiedIdentifierSyntax name = declarator.Names[0];
            return IsReferencedElsewhere<VB.IdentifierNameSyntax>(name, name.Identifier.ValueText, localDeclaration.Parent, semanticModel, cancellationToken)
                ? null
                : localDeclaration;
        }

        /// <summary>Una reasignación posterior (x = 5) dejaría la referencia huérfana al quitar la declaración.</summary>
        private static bool IsReferencedElsewhere<TIdentifier>(SyntaxNode declarator, string name, SyntaxNode? scope, SemanticModel semanticModel, CancellationToken cancellationToken)
            where TIdentifier : SyntaxNode
        {
            ISymbol? symbol = semanticModel.GetDeclaredSymbol(declarator, cancellationToken);
            if (symbol == null || scope == null)
                return true;

            // VB no distingue mayúsculas: se compara por símbolo, el nombre solo filtra candidatos.
            return scope.DescendantNodes()
                .OfType<TIdentifier>()
                .Where(identifier => string.Equals(identifier.ToString().Trim('@', '[', ']'), name, System.StringComparison.OrdinalIgnoreCase))
                .Any(identifier => SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol, symbol));
        }
    }
}
