using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Services.Whitespace;
using CSharpKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Text
{
    /// <summary>
    /// Usa el árbol de Roslyn para saber si un espacio es "entre tokens" o es parte de un string multilínea,
    /// un literal XML de VB o código deshabilitado (#if false / #If False).
    /// </summary>
    internal sealed class SyntaxTextGuard : ITextGuard
    {
        private readonly SyntaxNode _root;

        public SyntaxTextGuard(SyntaxNode root, IReadOnlyList<TextLineInfo> lines)
        {
            _root = root;
            Lines = lines;
        }

        public IReadOnlyList<TextLineInfo> Lines { get; }

        public static List<TextLineInfo> GetLines(SourceText text)
        {
            return text.Lines
                .Select(l => new TextLineInfo(l.Start, text.ToString(l.Span), text.ToString(TextSpan.FromBounds(l.End, l.EndIncludingLineBreak))))
                .ToList();
        }

        /// <summary>Línea en blanco que es trivia de solo espacios/saltos de línea (no está dentro de un token, comentario ni código deshabilitado).</summary>
        public bool CanRemoveLine(int lineIndex)
        {
            TextLineInfo line = Lines[lineIndex];
            foreach (int position in new[] { line.Start, line.Start + line.LengthIncludingLineBreak - 1 })
            {
                if (position < line.Start || position >= _root.FullSpan.End)
                    continue;

                if (IsInsideToken(position))
                    return false;

                SyntaxTrivia trivia = _root.FindTrivia(position, findInsideTrivia: true);
                if (trivia.RawKind == 0 || !string.IsNullOrWhiteSpace(trivia.ToString()))
                    return false;
            }

            return true;
        }

        /// <summary>Los espacios finales no son parte de un token (string multilínea) ni de código deshabilitado.</summary>
        public bool CanTrimLineEnd(int lineIndex)
        {
            TextLineInfo line = Lines[lineIndex];
            int position = line.Start + line.Text.TrimEnd().Length;

            if (position >= _root.FullSpan.End)
                return true;

            if (IsInsideToken(position))
                return false;

            SyntaxTrivia trivia = _root.FindTrivia(position, findInsideTrivia: true);
            return !IsDisabledText(trivia);
        }

        /// <summary>El inicio de la línea está entre tokens: ahí se puede insertar un salto de línea.</summary>
        public bool CanInsertLineBefore(int lineIndex)
        {
            int position = Lines[lineIndex].Start;
            if (position >= _root.FullSpan.End)
                return false;

            return !IsInsideToken(position) && !IsDisabledText(_root.FindTrivia(position, findInsideTrivia: true));
        }

        private bool IsInsideToken(int position)
        {
            SyntaxToken token = _root.FindToken(position, findInsideTrivia: true);
            return token.Span.Contains(position);
        }

        private bool IsDisabledText(SyntaxTrivia trivia)
        {
            return _root.Language == LanguageNames.CSharp
                ? trivia.RawKind == (int)CSharpKind.DisabledTextTrivia
                : trivia.RawKind == (int)VBKind.DisabledTextTrivia;
        }
    }
}
