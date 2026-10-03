using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Layout
{
    /// <summary>Líneas relevantes del archivo (índices 0-based), calculadas por el lenguaje.</summary>
    internal sealed class LayoutAnalysis
    {
        /// <summary>Línea que abre un bloque: termina en "{" (C#) o es el encabezado de un bloque (VB: Sub, If, For…).</summary>
        public List<int> OpenerLines { get; } = new List<int>();

        /// <summary>Línea que cierra un bloque: empieza con "}" (C#) o con End/Next/Loop/Else/Catch/Finally (VB).</summary>
        public List<int> CloserLines { get; } = new List<int>();

        /// <summary>Línea donde termina una lista de atributos y sigue el miembro en otra línea.</summary>
        public List<int> AttributeEndLines { get; } = new List<int>();

        /// <summary>Línea que empieza con ".Metodo()" de una llamada encadenada.</summary>
        public List<int> ChainedCallLines { get; } = new List<int>();

        /// <summary>Primera línea (incluye comentarios encima) de un miembro que debe separarse del anterior.</summary>
        public List<int> MemberStartLines { get; } = new List<int>();

        /// <summary>Líneas de #region / #endregion.</summary>
        public List<int> RegionLines { get; } = new List<int>();

        /// <summary>Primera línea de cada case (excepto el primero) de un switch / Select Case.</summary>
        public List<int> CaseStartLines { get; } = new List<int>();

        /// <summary>Líneas con un comentario de una línea que está encima de una sentencia o un miembro.</summary>
        public List<int> CommentLines { get; } = new List<int>();

        /// <summary>Línea del último using / Imports de cada bloque de usings.</summary>
        public List<int> LastUsingLines { get; } = new List<int>();
    }

    internal interface ILayoutSyntax
    {
        LayoutAnalysis Analyze(SyntaxNode root, SourceText text, MemberKind blankLineKinds);
    }

    internal static class LayoutSyntax
    {
        private static readonly ILayoutSyntax CSharp = new CSharpLayoutSyntax();
        private static readonly ILayoutSyntax VisualBasic = new VisualBasicLayoutSyntax();

        public static ILayoutSyntax? For(string language) => language switch
        {
            LanguageNames.CSharp => CSharp,
            LanguageNames.VisualBasic => VisualBasic,
            _ => null
        };

        public static int LineOf(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;

        /// <summary>El token es lo último de su línea (lo que sigue está en otra línea).</summary>
        public static bool IsLastOnLine(SourceText text, SyntaxToken token)
        {
            SyntaxToken next = token.GetNextToken();
            return next.RawKind == 0 || LineOf(text, next.SpanStart) > LineOf(text, token.Span.End);
        }

        /// <summary>El token es lo primero de su línea (solo hay espacios antes).</summary>
        public static bool IsFirstOnLine(SourceText text, SyntaxToken token)
        {
            TextLine line = text.Lines.GetLineFromPosition(token.SpanStart);
            return string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(line.Start, token.SpanStart)));
        }

        /// <summary>
        /// Primera línea de un nodo contando los comentarios que tiene encima. null si encima hay directivas (#if, #region):
        /// ahí no se agrega nada para no mover código entre directivas.
        /// </summary>
        public static int? StartLineWithComments(SourceText text, SyntaxNode node)
        {
            int position = node.SpanStart;
            foreach (SyntaxTrivia trivia in node.GetLeadingTrivia())
            {
                if (trivia.IsDirective)
                    return null;

                if (!string.IsNullOrWhiteSpace(trivia.ToFullString()))
                {
                    position = trivia.SpanStart;
                    break;
                }
            }

            return LineOf(text, position);
        }

        /// <summary>Agrega la línea de inicio de cada miembro (salvo el primero) que debe separarse del anterior.</summary>
        public static void AddMemberStartLines<TMember>(
            LayoutAnalysis analysis,
            SourceText text,
            IReadOnlyList<TMember> members,
            System.Func<TMember, MemberKind> getKind,
            MemberKind blankLineKinds) where TMember : SyntaxNode
        {
            for (int i = 1; i < members.Count; i++)
            {
                if ((getKind(members[i]) & blankLineKinds) == 0 && (getKind(members[i - 1]) & blankLineKinds) == 0)
                    continue;

                int? start = StartLineWithComments(text, members[i]);
                if (start.HasValue && start.Value > LineOf(text, members[i - 1].Span.End))
                {
                    analysis.MemberStartLines.Add(start.Value);
                }
            }
        }
    }
}
