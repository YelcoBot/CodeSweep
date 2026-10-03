using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Options;
using static YelcoBot.CodeSweep.Infrastructure.Roslyn.Layout.LayoutSyntax;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Layout
{
    internal sealed class CSharpLayoutSyntax : ILayoutSyntax
    {
        public LayoutAnalysis Analyze(SyntaxNode root, SourceText text, MemberKind blankLineKinds)
        {
            LayoutAnalysis analysis = new LayoutAnalysis();

            foreach (SyntaxToken token in root.DescendantTokens())
            {
                AnalyzeToken(analysis, text, token);
            }

            foreach (SyntaxNode node in root.DescendantNodesAndSelf())
            {
                AnalyzeNode(analysis, text, node, blankLineKinds);
            }

            foreach (SyntaxTrivia trivia in root.DescendantTrivia())
            {
                AnalyzeTrivia(analysis, text, trivia);
            }

            return analysis;
        }

        private static void AnalyzeToken(LayoutAnalysis analysis, SourceText text, SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.OpenBraceToken when IsLastOnLine(text, token):
                    analysis.OpenerLines.Add(LineOf(text, token.SpanStart));
                    break;

                case SyntaxKind.CloseBraceToken when IsFirstOnLine(text, token):
                    analysis.CloserLines.Add(LineOf(text, token.SpanStart));
                    break;

                // [assembly: …] no: la línea en blanco que lo separa del namespace es normal.
                case SyntaxKind.CloseBracketToken when token.Parent is AttributeListSyntax { Target: null } && IsLastOnLine(text, token):
                    analysis.AttributeEndLines.Add(LineOf(text, token.SpanStart));
                    break;

                case SyntaxKind.DotToken when token.Parent is MemberAccessExpressionSyntax && IsFirstOnLine(text, token):
                case SyntaxKind.QuestionToken when token.Parent is ConditionalAccessExpressionSyntax && IsFirstOnLine(text, token):
                    analysis.ChainedCallLines.Add(LineOf(text, token.SpanStart));
                    break;
            }
        }

        private static void AnalyzeNode(LayoutAnalysis analysis, SourceText text, SyntaxNode node, MemberKind blankLineKinds)
        {
            switch (node)
            {
                case CompilationUnitSyntax unit:
                    AddMemberStartLines(analysis, text, unit.Members, GetKind, blankLineKinds);
                    AddLastUsing(analysis, text, unit.Usings);
                    break;

                case BaseNamespaceDeclarationSyntax ns:
                    AddMemberStartLines(analysis, text, ns.Members, GetKind, blankLineKinds);
                    AddLastUsing(analysis, text, ns.Usings);
                    break;

                case TypeDeclarationSyntax type:
                    AddMemberStartLines(analysis, text, type.Members, GetKind, blankLineKinds);
                    break;

                case SwitchStatementSyntax switchStatement:
                    foreach (SwitchSectionSyntax section in switchStatement.Sections.Skip(1))
                    {
                        if (StartLineWithComments(text, section) is int line)
                        {
                            analysis.CaseStartLines.Add(line);
                        }
                    }

                    break;
            }
        }

        private static void AnalyzeTrivia(LayoutAnalysis analysis, SourceText text, SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.RegionDirectiveTrivia:
                case SyntaxKind.EndRegionDirectiveTrivia:
                    analysis.RegionLines.Add(LineOf(text, trivia.SpanStart));
                    break;

                case SyntaxKind.SingleLineCommentTrivia when IsCommentAboveStatementOrMember(text, trivia):
                    analysis.CommentLines.Add(LineOf(text, trivia.SpanStart));
                    break;
            }
        }

        /// <summary>Comentario solo en su línea, encima de una sentencia o un miembro (no dentro de argumentos, inicializadores…).</summary>
        private static bool IsCommentAboveStatementOrMember(SourceText text, SyntaxTrivia trivia)
        {
            SyntaxToken token = trivia.Token;
            if (!token.LeadingTrivia.Span.Contains(trivia.Span) || !IsTriviaFirstOnLine(text, trivia))
                return false;

            SyntaxNode? owner = token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n is StatementSyntax or MemberDeclarationSyntax);
            return owner != null && owner.GetFirstToken() == token;
        }

        private static bool IsTriviaFirstOnLine(SourceText text, SyntaxTrivia trivia)
        {
            TextLine line = text.Lines.GetLineFromPosition(trivia.SpanStart);
            return string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(line.Start, trivia.SpanStart)));
        }

        private static void AddLastUsing(LayoutAnalysis analysis, SourceText text, SyntaxList<UsingDirectiveSyntax> usings)
        {
            if (usings.Count > 0)
            {
                analysis.LastUsingLines.Add(LineOf(text, usings.Last().Span.End));
            }
        }

        private static MemberKind GetKind(MemberDeclarationSyntax member) => member switch
        {
            FieldDeclarationSyntax => MemberKind.Fields,
            ConstructorDeclarationSyntax or DestructorDeclarationSyntax => MemberKind.Constructors,
            PropertyDeclarationSyntax or IndexerDeclarationSyntax => MemberKind.Properties,
            MethodDeclarationSyntax or OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax => MemberKind.Methods,
            EventDeclarationSyntax or EventFieldDeclarationSyntax => MemberKind.Events,
            EnumDeclarationSyntax => MemberKind.Enums,
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or BaseNamespaceDeclarationSyntax => MemberKind.Types,
            _ => MemberKind.None
        };
    }
}
