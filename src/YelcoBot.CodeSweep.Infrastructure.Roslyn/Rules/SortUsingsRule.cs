using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using VB = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Ordena los usings (C#: del archivo y de cada namespace) y los Imports (VB).
    /// C#: global → normales → static → alias. VB: normales → alias → XML.
    /// En ambos: System.* primero; luego alfabético. El comentario de encabezado del primero se queda arriba.
    /// </summary>
    public class SortUsingsRule : ISweepRule
    {
        public string Id => "SORT_USINGS";

        public int Order => 30;

        public bool IsEnabled(SweepOptions options) => options.EnableSortUsings;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);

            SyntaxNode? newRoot = syntaxRoot switch
            {
                CompilationUnitSyntax compilationUnit => SortCSharp(compilationUnit),
                VB.CompilationUnitSyntax compilationUnit => SortVisualBasic(compilationUnit),
                _ => null
            };

            return newRoot == null || newRoot == syntaxRoot ? document : document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode SortCSharp(CompilationUnitSyntax compilationUnit)
        {
            return compilationUnit.ReplaceNodes(
                compilationUnit.DescendantNodesAndSelf().Where(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax),
                (original, rewritten) => rewritten switch
                {
                    CompilationUnitSyntax unit => SortUsings(unit.Usings) is { } sorted ? unit.WithUsings(sorted) : unit,
                    BaseNamespaceDeclarationSyntax ns => SortUsings(ns.Usings) is { } sorted ? ns.WithUsings(sorted) : ns,
                    _ => rewritten
                });
        }

        private static SyntaxNode SortVisualBasic(VB.CompilationUnitSyntax compilationUnit)
        {
            SyntaxList<VB.ImportsStatementSyntax> imports = compilationUnit.Imports;
            if (!CanSort(imports))
                return compilationUnit;

            List<VB.ImportsStatementSyntax> sorted = imports
                .OrderBy(GetKindOrder)
                .ThenBy(i => IsSystem(GetName(i)) ? 0 : 1)
                .ThenBy(GetName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(GetName, StringComparer.Ordinal)
                .ToList();

            return sorted.SequenceEqual(imports)
                ? compilationUnit
                : compilationUnit.WithImports(VBSyntaxFactory.List(KeepHeaderOnTop(imports, sorted)));
        }

        /// <summary>Devuelve la lista ordenada, o null si no hay nada que cambiar o no es seguro reordenar.</summary>
        private static SyntaxList<UsingDirectiveSyntax>? SortUsings(SyntaxList<UsingDirectiveSyntax> usings)
        {
            if (!CanSort(usings))
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

            return SyntaxFactory.List(KeepHeaderOnTop(usings, sorted));
        }

        /// <summary>Directivas (#if, #region) entre usings: reordenar rompería la estructura.</summary>
        private static bool CanSort<TNode>(SyntaxList<TNode> usings) where TNode : SyntaxNode
        {
            return usings.Count > 1 && !usings.Skip(1).Any(u => u.GetLeadingTrivia().Any(t => t.IsDirective));
        }

        /// <summary>El header (comentarios encima del primer using) se queda arriba aunque ese using cambie de lugar.</summary>
        private static List<TNode> KeepHeaderOnTop<TNode>(SyntaxList<TNode> original, List<TNode> sorted) where TNode : SyntaxNode
        {
            SyntaxTriviaList header = original[0].GetLeadingTrivia();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i] == original[0])
                {
                    sorted[i] = sorted[i].WithLeadingTrivia(SyntaxTriviaList.Empty);
                }
            }

            sorted[0] = sorted[0].WithLeadingTrivia(header.AddRange(sorted[0].GetLeadingTrivia()));
            return sorted;
        }

        private static int GetKindOrder(UsingDirectiveSyntax u)
        {
            if (u.Alias != null)
                return 2;

            return u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) ? 1 : 0;
        }

        private static int GetKindOrder(VB.ImportsStatementSyntax i)
        {
            return i.ImportsClauses.FirstOrDefault() switch
            {
                VB.SimpleImportsClauseSyntax { Alias: null } => 0,
                VB.SimpleImportsClauseSyntax => 1,
                _ => 2
            };
        }

        private static string GetName(UsingDirectiveSyntax u)
        {
            return u.Alias?.Name.Identifier.ValueText ?? u.NamespaceOrType.ToString();
        }

        private static string GetName(VB.ImportsStatementSyntax i)
        {
            return i.ImportsClauses.FirstOrDefault() switch
            {
                VB.SimpleImportsClauseSyntax clause => clause.Alias?.Identifier.ValueText ?? clause.Name.ToString(),
                { } clause => clause.ToString(),
                null => string.Empty
            };
        }

        private static bool IsSystem(string name)
        {
            return name == "System" || name.StartsWith("System.", StringComparison.Ordinal)
                || name.StartsWith("Global.System", StringComparison.OrdinalIgnoreCase);
        }
    }
}
