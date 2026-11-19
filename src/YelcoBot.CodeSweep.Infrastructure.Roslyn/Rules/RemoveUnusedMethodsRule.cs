using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.UnusedMethods;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Removes C# and VB methods that nothing in the solution references.
    /// Which accessibility levels are candidates and which protections apply come from <see cref="SweepOptions"/>;
    /// see <see cref="UnusedMethodPolicy"/> for the fixed exclusions.
    /// Runs several passes per document, so a method only called by another removed method goes too.
    /// Skipped when the document has compile errors.
    /// </summary>
    public class RemoveUnusedMethodsRule : ISweepRule
    {
        private const int MaxPasses = 5;

        private readonly MarkupIdentifierIndex _markupIndex;
        private readonly SolutionNameIndex _solutionNameIndex;

        public RemoveUnusedMethodsRule(MarkupIdentifierIndex markupIndex, SolutionNameIndex solutionNameIndex)
        {
            _markupIndex = markupIndex;
            _solutionNameIndex = solutionNameIndex;
        }

        public string Id => "REMOVE_UNUSED_METHODS";

        public int Order => 5;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveUnusedMethods;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            if (document.Project.Language is not (LanguageNames.CSharp or LanguageNames.VisualBasic))
                return document;

            Solution originalSolution = document.Project.Solution;
            SolutionNames solutionNames = await _solutionNameIndex.GetNamesAsync(originalSolution).ConfigureAwait(false);
            ISet<string> markupNames = options.UnusedMethodsProtectMarkupReferences
                ? _markupIndex.GetIdentifiers(originalSolution, options.UnusedMethodsMarkupExtensions)
                : new HashSet<string>();

            for (int pass = 0; pass < MaxPasses; pass++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Document? cleaned = await RemoveOnceAsync(document, options, solutionNames, markupNames, cancellationToken).ConfigureAwait(false);
                if (cleaned == null)
                    break;

                document = cleaned;
            }

            return document;
        }

        private static async Task<Document?> RemoveOnceAsync(
            Document document,
            SweepOptions options,
            SolutionNames solutionNames,
            ISet<string> markupNames,
            CancellationToken cancellationToken)
        {
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null || root == null)
                return null;

            if (semanticModel.GetDiagnostics(cancellationToken: cancellationToken).Any(d => d.Severity == DiagnosticSeverity.Error))
                return null;

            Solution solution = document.Project.Solution;
            bool hasExternalFriends = UnusedMethodPolicy.HasExternalFriends(semanticModel.Compilation.Assembly, solution);
            List<SyntaxNode> nodesToRemove = new List<SyntaxNode>();

            foreach (SyntaxNode declaration in root.DescendantNodes().Where(n => n is CSSyntax.MethodDeclarationSyntax or VBSyntax.MethodBlockSyntax))
            {
                SyntaxNode symbolNode = declaration is VBSyntax.MethodBlockSyntax block ? block.SubOrFunctionStatement : declaration;
                if (semanticModel.GetDeclaredSymbol(symbolNode, cancellationToken) is not IMethodSymbol method)
                    continue;

                if (!UnusedMethodPolicy.IsRemovableKind(method))
                    continue;

                Accessibility accessibility = UnusedMethodPolicy.GetEffectiveAccessibility(method);
                if (!UnusedMethodPolicy.IsAccessibilityIncluded(accessibility, options, hasExternalFriends))
                    continue;

                if (UnusedMethodPolicy.IsProtected(method, options, solutionNames, markupNames))
                    continue;

                if (!await IsReferencedAsync(method, declaration, accessibility, solution, cancellationToken).ConfigureAwait(false))
                {
                    nodesToRemove.Add(declaration);
                }
            }

            if (nodesToRemove.Count == 0)
                return null;

            SyntaxNode? newRoot = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            return newRoot == null ? null : document.WithSyntaxRoot(newRoot);
        }

        /// <summary>Any reference outside the method's own body counts; a recursive call alone does not.</summary>
        private static async Task<bool> IsReferencedAsync(
            IMethodSymbol method,
            SyntaxNode declaration,
            Accessibility accessibility,
            Solution solution,
            CancellationToken cancellationToken)
        {
            IImmutableSet<Document>? scope = accessibility == Accessibility.Private ? GetOutermostTypeDocuments(method, solution) : null;
            IEnumerable<ReferencedSymbol> references = await SymbolFinder.FindReferencesAsync(method, solution, scope, cancellationToken).ConfigureAwait(false);

            return references
                .SelectMany(r => r.Locations)
                .Any(location => !IsInside(location.Location, declaration));
        }

        private static bool IsInside(Location location, SyntaxNode declaration)
        {
            return location.SourceTree == declaration.SyntaxTree && declaration.Span.Contains(location.SourceSpan);
        }

        /// <summary>A private member is visible to its own type and every type nested in the outermost one, across partial files.</summary>
        private static IImmutableSet<Document>? GetOutermostTypeDocuments(IMethodSymbol method, Solution solution)
        {
            INamedTypeSymbol type = method.ContainingType;
            while (type.ContainingType != null)
            {
                type = type.ContainingType;
            }

            ImmutableHashSet<Document>.Builder documents = ImmutableHashSet.CreateBuilder<Document>();
            foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
            {
                Document? document = solution.GetDocument(reference.SyntaxTree);
                if (document == null)
                    return null;

                documents.Add(document);
            }

            return documents.Count == 0 ? null : documents.ToImmutable();
        }
    }
}
