using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.UnusedMethods
{
    /// <summary>
    /// Names that the C# / VB code of the solution mentions where Roslyn finds no reference:
    /// inside string literals (reflection, Invoke("Name"), SelectMethod = "GetItems") and inside inactive #if blocks.
    /// Built once per solution snapshot and shared by every document cleaned in parallel.
    /// </summary>
    public sealed class SolutionNameIndex
    {
        private readonly ConditionalWeakTable<Solution, Lazy<Task<SolutionNames>>> _cache = new ConditionalWeakTable<Solution, Lazy<Task<SolutionNames>>>();

        public Task<SolutionNames> GetNamesAsync(Solution solution)
        {
            return _cache.GetValue(solution, s => new Lazy<Task<SolutionNames>>(() => Task.Run(() => BuildAsync(s)))).Value;
        }

        private static async Task<SolutionNames> BuildAsync(Solution solution)
        {
            HashSet<string> stringNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> disabledCodeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Project project in solution.Projects.Where(p => p.Language is LanguageNames.CSharp or LanguageNames.VisualBasic))
            {
                foreach (Document document in project.Documents)
                {
                    SyntaxNode? root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                    if (root == null)
                        continue;

                    foreach (SyntaxNode node in root.DescendantNodes())
                    {
                        if (IsStringText(node))
                        {
                            stringNames.UnionWith(MarkupIdentifierIndex.Tokenize(node.ToString()));
                        }
                    }

                    if (!root.ContainsDirectives)
                        continue;

                    foreach (SyntaxTrivia trivia in root.DescendantTrivia())
                    {
                        if (trivia.IsKind(CS.SyntaxKind.DisabledTextTrivia) || trivia.IsKind(VB.SyntaxKind.DisabledTextTrivia))
                        {
                            disabledCodeNames.UnionWith(MarkupIdentifierIndex.Tokenize(trivia.ToString()));
                        }
                    }
                }
            }

            return new SolutionNames(stringNames, disabledCodeNames);
        }

        private static bool IsStringText(SyntaxNode node)
        {
            return node switch
            {
                CSSyntax.LiteralExpressionSyntax literal => literal.IsKind(CS.SyntaxKind.StringLiteralExpression) || literal.IsKind(CS.SyntaxKind.Utf8StringLiteralExpression),
                CSSyntax.InterpolatedStringTextSyntax => true,
                VBSyntax.LiteralExpressionSyntax literal => literal.IsKind(VB.SyntaxKind.StringLiteralExpression),
                VBSyntax.InterpolatedStringTextSyntax => true,
                _ => false
            };
        }
    }

    public sealed class SolutionNames
    {
        public SolutionNames(ISet<string> stringNames, ISet<string> disabledCodeNames)
        {
            StringNames = stringNames;
            DisabledCodeNames = disabledCodeNames;
        }

        public ISet<string> StringNames { get; }

        public ISet<string> DisabledCodeNames { get; }
    }
}
