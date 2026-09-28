using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Ordena los usings (del archivo y de cada namespace): global → normales → static → alias; System.* primero; luego alfabético.
    /// El comentario de encabezado del primer using se queda arriba.
    /// </summary>
    public class SortUsingsRule : ISweepRule
    {
        public string Id => "CS_SORT_USINGS";
        public int Order => 30;

        public bool IsEnabled(SweepOptions options) => options.EnableSortUsings;

        public async Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default)
        {
            if (document.Project.Language != LanguageNames.CSharp)
                return document;

            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot is not CompilationUnitSyntax compilationUnit)
                return document;

            SyntaxNode newRoot = syntaxRoot.ReplaceNodes(
                compilationUnit.DescendantNodesAndSelf().Where(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax),
                (original, rewritten) => rewritten switch
                {
                    CompilationUnitSyntax unit => SortUsings(unit.Usings) is { } sorted ? unit.WithUsings(sorted) : unit,
                    BaseNamespaceDeclarationSyntax ns => SortUsings(ns.Usings) is { } sorted ? ns.WithUsings(sorted) : ns,
                    _ => rewritten
                });

            return newRoot == syntaxRoot ? document : document.WithSyntaxRoot(newRoot);
        }

        /// <summary>Devuelve la lista ordenada, o null si no hay nada que cambiar o no es seguro reordenar.</summary>
        private static SyntaxList<UsingDirectiveSyntax>? SortUsings(SyntaxList<UsingDirectiveSyntax> usings)
        {
            if (usings.Count <= 1)
                return null;

            // Directivas (#if, #region) entre usings: reordenar rompería la estructura.
            if (usings.Skip(1).Any(u => u.GetLeadingTrivia().Any(t => t.IsDirective)))
                return null;

            List<UsingDirectiveSyntax> sorted = usings
                .OrderBy(u => u.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword) ? 0 : 1)
                .ThenBy(GetKindOrder)
                .ThenBy(u => IsSystem(GetName(u)) ? 0 : 1)
                .ThenBy(GetName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(GetName, StringComparer.Ordinal)
                .ToList();

            if (sorted.SequenceEqual(usings))
                return null;

            // El header (comentarios encima del primer using) se queda arriba aunque ese using cambie de lugar.
            SyntaxTriviaList header = usings[0].GetLeadingTrivia();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i] == usings[0])
                {
                    sorted[i] = sorted[i].WithLeadingTrivia(SyntaxTriviaList.Empty);
                }
            }

            sorted[0] = sorted[0].WithLeadingTrivia(header.AddRange(sorted[0].GetLeadingTrivia()));
            return SyntaxFactory.List(sorted);
        }

        private static int GetKindOrder(UsingDirectiveSyntax u)
        {
            if (u.Alias != null)
                return 2;

            return u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) ? 1 : 0;
        }

        private static string GetName(UsingDirectiveSyntax u)
        {
            return u.Alias?.Name.Identifier.ValueText ?? u.NamespaceOrType.ToString();
        }

        private static bool IsSystem(string name)
        {
            return name == "System" || name.StartsWith("System.", StringComparison.Ordinal);
        }
    }
}
