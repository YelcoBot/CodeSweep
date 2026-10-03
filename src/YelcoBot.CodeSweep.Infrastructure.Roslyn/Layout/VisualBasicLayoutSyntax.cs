using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using static YelcoBot.CodeSweep.Infrastructure.Roslyn.Layout.LayoutSyntax;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Layout
{
    internal sealed class VisualBasicLayoutSyntax : ILayoutSyntax
    {
        public LayoutAnalysis Analyze(SyntaxNode root, SourceText text, MemberKind blankLineKinds)
        {
            LayoutAnalysis analysis = new LayoutAnalysis();

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

        private static void AnalyzeNode(LayoutAnalysis analysis, SourceText text, SyntaxNode node, MemberKind blankLineKinds)
        {
            if (GetBeginStatement(node) is { } begin)
            {
                analysis.OpenerLines.Add(LineOf(text, begin.Span.End));
            }

            switch (node)
            {
                // Cierran el cuerpo anterior: End …, Next, Loop, Else, ElseIf, Catch, Finally.
                case EndBlockStatementSyntax or NextStatementSyntax or LoopStatementSyntax
                    or ElseStatementSyntax or ElseIfStatementSyntax or CatchStatementSyntax or FinallyStatementSyntax
                    when IsFirstOnLine(text, node.GetFirstToken()):
                    analysis.CloserLines.Add(LineOf(text, node.SpanStart));
                    break;

                // <Assembly: …> no: la línea en blanco que lo separa del resto es normal.
                case AttributeListSyntax attributes when attributes.Parent is not AttributesStatementSyntax
                    && IsLastOnLine(text, attributes.GreaterThanToken):
                    analysis.AttributeEndLines.Add(LineOf(text, attributes.GreaterThanToken.SpanStart));
                    break;

                case CompilationUnitSyntax unit:
                    AddMemberStartLines(analysis, text, unit.Members, GetKind, blankLineKinds);
                    if (unit.Imports.Count > 0)
                    {
                        analysis.LastUsingLines.Add(LineOf(text, unit.Imports.Last().Span.End));
                    }

                    break;

                case NamespaceBlockSyntax ns:
                    AddMemberStartLines(analysis, text, ns.Members, GetKind, blankLineKinds);
                    break;

                case TypeBlockSyntax type:
                    AddMemberStartLines(analysis, text, type.Members, GetKind, blankLineKinds);
                    break;

                case SelectBlockSyntax select:
                    foreach (CaseBlockSyntax caseBlock in select.CaseBlocks.Skip(1))
                    {
                        if (StartLineWithComments(text, caseBlock) is int line)
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

                case SyntaxKind.CommentTrivia when IsCommentAboveStatement(text, trivia):
                    analysis.CommentLines.Add(LineOf(text, trivia.SpanStart));
                    break;
            }
        }

        /// <summary>Comentario solo en su línea, encima de una sentencia (en VB los miembros también son sentencias).</summary>
        private static bool IsCommentAboveStatement(SourceText text, SyntaxTrivia trivia)
        {
            SyntaxToken token = trivia.Token;
            TextLine line = text.Lines.GetLineFromPosition(trivia.SpanStart);

            if (!token.LeadingTrivia.Span.Contains(trivia.Span)
                || !string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(line.Start, trivia.SpanStart))))
                return false;

            SyntaxNode? owner = token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n is StatementSyntax);
            return owner != null && owner.GetFirstToken() == token;
        }

        /// <summary>Encabezado de un bloque (Sub, Class, If, For, Try, Case…): lo que viene después es el cuerpo.</summary>
        private static SyntaxNode? GetBeginStatement(SyntaxNode node) => node switch
        {
            MethodBlockBaseSyntax block => block.BlockStatement,
            TypeBlockSyntax block => block.BlockStatement,
            EnumBlockSyntax block => block.EnumStatement,
            NamespaceBlockSyntax block => block.NamespaceStatement,
            PropertyBlockSyntax block => block.PropertyStatement,
            EventBlockSyntax block => block.EventStatement,
            MultiLineIfBlockSyntax block => block.IfStatement,
            ElseIfBlockSyntax block => block.ElseIfStatement,
            ElseBlockSyntax block => block.ElseStatement,
            ForOrForEachBlockSyntax block => block.ForOrForEachStatement,
            WhileBlockSyntax block => block.WhileStatement,
            DoLoopBlockSyntax block => block.DoStatement,
            UsingBlockSyntax block => block.UsingStatement,
            SyncLockBlockSyntax block => block.SyncLockStatement,
            WithBlockSyntax block => block.WithStatement,
            TryBlockSyntax block => block.TryStatement,
            CatchBlockSyntax block => block.CatchStatement,
            FinallyBlockSyntax block => block.FinallyStatement,
            SelectBlockSyntax block => block.SelectStatement,
            CaseBlockSyntax block => block.CaseStatement,
            MultiLineLambdaExpressionSyntax lambda => lambda.SubOrFunctionHeader,
            _ => null
        };

        private static MemberKind GetKind(StatementSyntax member) => member switch
        {
            FieldDeclarationSyntax => MemberKind.Fields,
            ConstructorBlockSyntax => MemberKind.Constructors,
            PropertyBlockSyntax or PropertyStatementSyntax => MemberKind.Properties,
            MethodBlockSyntax or OperatorBlockSyntax or MethodStatementSyntax or DeclareStatementSyntax => MemberKind.Methods,
            EventBlockSyntax or EventStatementSyntax => MemberKind.Events,
            EnumBlockSyntax => MemberKind.Enums,
            TypeBlockSyntax or DelegateStatementSyntax or NamespaceBlockSyntax => MemberKind.Types,
            _ => MemberKind.None
        };
    }
}
