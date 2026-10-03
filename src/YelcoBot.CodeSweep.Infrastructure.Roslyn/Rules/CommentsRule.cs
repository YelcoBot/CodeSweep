using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using CSharpKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Comentarios de una línea de C# (//) y VB ('):
    /// 6.2 espacio después del prefijo (//Texto → // Texto) y 6.1 partir los que pasan del ancho configurado.
    /// No toca comentarios XML (/// y '''), separadores (////, '''') ni comentarios al final de una línea de código (6.1).
    /// </summary>
    public class CommentsRule : ISweepRule
    {
        /// <summary>Un tab cuenta como 4 columnas al medir el ancho.</summary>
        private const int TabWidth = 4;

        public string Id => "COMMENTS";

        public int Order => 45;

        public bool IsEnabled(SweepOptions options) => options.EnableSpaceAfterCommentPrefix || options.EnableWrapComments;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot == null || syntaxRoot.Language is not (LanguageNames.CSharp or LanguageNames.VisualBasic))
                return document;

            bool isCSharp = syntaxRoot.Language == LanguageNames.CSharp;
            string prefix = isCSharp ? "//" : "'";
            int commentKind = isCSharp ? (int)CSharpKind.SingleLineCommentTrivia : (int)VBKind.CommentTrivia;

            SourceText text = await document.GetTextAsync(cancellationToken);
            string lineBreak = text.Lines.Count > 1 ? text.ToString(TextSpan.FromBounds(text.Lines[0].End, text.Lines[0].EndIncludingLineBreak)) : "\r\n";
            List<TextChange> changes = new List<TextChange>();

            foreach (SyntaxTrivia trivia in syntaxRoot.DescendantTrivia().Where(t => t.RawKind == commentKind))
            {
                string comment = trivia.ToString();

                // Solo comentarios con el prefijo exacto: no ///, ////, ''', REM…
                if (!comment.StartsWith(prefix) || comment.Length == prefix.Length || comment.Substring(prefix.Length).StartsWith(prefix.Substring(0, 1)))
                    continue;

                string body = comment.Substring(prefix.Length).Trim();
                if (body.Length == 0)
                    continue;

                string fixedComment = options.EnableSpaceAfterCommentPrefix && !char.IsWhiteSpace(comment[prefix.Length])
                    ? prefix + " " + comment.Substring(prefix.Length)
                    : comment;

                string replacement = options.EnableWrapComments && IsAloneOnLine(text, trivia)
                    ? Wrap(text, trivia, fixedComment, prefix, options.CommentWrapColumn, lineBreak)
                    : fixedComment;

                if (replacement != comment)
                {
                    changes.Add(new TextChange(trivia.Span, replacement));
                }
            }

            return changes.Count == 0 ? document : document.WithText(text.WithChanges(changes));
        }

        private static bool IsAloneOnLine(SourceText text, SyntaxTrivia trivia)
        {
            TextLine line = text.Lines.GetLineFromPosition(trivia.SpanStart);
            return string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(line.Start, trivia.SpanStart)));
        }

        /// <summary>
        /// Parte el comentario en varias líneas con la misma sangría y prefijo, sin cortar palabras.
        /// Solo parte (no une líneas cortas): así no se pierde el formato que el autor le dio.
        /// </summary>
        private static string Wrap(SourceText text, SyntaxTrivia trivia, string comment, string prefix, int column, string lineBreak)
        {
            TextLine line = text.Lines.GetLineFromPosition(trivia.SpanStart);
            string indentation = text.ToString(TextSpan.FromBounds(line.Start, trivia.SpanStart));
            int indentationWidth = indentation.Sum(c => c == '\t' ? TabWidth : 1);

            if (column <= 0 || indentationWidth + comment.Length <= column)
                return comment;

            string body = comment.Substring(prefix.Length);
            string spacing = body.Substring(0, body.Length - body.TrimStart().Length);
            if (spacing.Length == 0)
            {
                spacing = " ";
            }

            string linePrefix = prefix + spacing;
            int available = column - indentationWidth - linePrefix.Length;
            if (available < 10)
                return comment;

            List<string> lines = new List<string>();
            StringBuilder current = new StringBuilder();

            foreach (string word in body.Trim().Split(' ').Where(w => w.Length > 0))
            {
                if (current.Length > 0 && current.Length + 1 + word.Length > available)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }

                if (current.Length > 0)
                {
                    current.Append(' ');
                }

                current.Append(word);
            }

            if (current.Length > 0)
            {
                lines.Add(current.ToString());
            }

            return string.Join(lineBreak + indentation, lines.Select(l => linePrefix + l));
        }
    }
}
