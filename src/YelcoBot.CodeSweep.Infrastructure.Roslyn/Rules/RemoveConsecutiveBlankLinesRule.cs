using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Deja como máximo una línea en blanco seguida. Trabaja solo sobre la trivia (espacios entre tokens),
    /// así nunca modifica strings multilínea, raw strings ni código deshabilitado (#if false).
    /// </summary>
    public class RemoveConsecutiveBlankLinesRule : ISweepRule
    {
        private const int MaxConsecutiveBlankLines = 1;

        public string Id => "CS_REMOVE_BLANK_LINES";
        public int Order => 40;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveConsecutiveBlankLines;

        public async Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default)
        {
            if (document.Project.Language != LanguageNames.CSharp)
                return document;

            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot == null)
                return document;

            Dictionary<SyntaxToken, SyntaxToken> replacements = new Dictionary<SyntaxToken, SyntaxToken>();
            foreach (SyntaxToken token in syntaxRoot.DescendantTokens(descendIntoTrivia: false))
            {
                if (!token.HasLeadingTrivia || token.LeadingTrivia.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) <= MaxConsecutiveBlankLines)
                    continue;

                SyntaxTriviaList collapsed = CollapseBlankLines(token.LeadingTrivia);
                if (collapsed.Count != token.LeadingTrivia.Count)
                {
                    replacements[token] = token.WithLeadingTrivia(collapsed);
                }
            }

            if (replacements.Count == 0)
                return document;

            SyntaxNode newRoot = syntaxRoot.ReplaceTokens(replacements.Keys, (original, _) => replacements[original]);
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// La trivia inicial de un token empieza al comienzo de una línea (la anterior terminó con su salto de línea).
        /// Una línea en blanco = solo espacios + salto de línea, estando al inicio de línea.
        /// </summary>
        private static SyntaxTriviaList CollapseBlankLines(SyntaxTriviaList trivia)
        {
            List<SyntaxTrivia> result = new List<SyntaxTrivia>();
            List<SyntaxTrivia> pendingWhitespace = new List<SyntaxTrivia>();
            bool atLineStart = true;
            int blankLines = 0;

            foreach (SyntaxTrivia item in trivia)
            {
                if (item.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    pendingWhitespace.Add(item);
                    continue;
                }

                if (item.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    if (atLineStart)
                    {
                        blankLines++;
                        if (blankLines <= MaxConsecutiveBlankLines)
                        {
                            result.Add(item);
                        }

                        // Los espacios de una línea en blanco sobran siempre.
                        pendingWhitespace.Clear();
                    }
                    else
                    {
                        result.AddRange(pendingWhitespace);
                        pendingWhitespace.Clear();
                        result.Add(item);
                        atLineStart = true;
                    }

                    continue;
                }

                // Comentario, directiva, doc comment…: es una línea con contenido.
                result.AddRange(pendingWhitespace);
                pendingWhitespace.Clear();
                result.Add(item);
                blankLines = 0;

                // Directivas y doc comments incluyen su propio salto de línea.
                atLineStart = item.GetStructure() is DirectiveTriviaSyntax or DocumentationCommentTriviaSyntax;
            }

            result.AddRange(pendingWhitespace);
            return SyntaxFactory.TriviaList(result);
        }
    }
}
