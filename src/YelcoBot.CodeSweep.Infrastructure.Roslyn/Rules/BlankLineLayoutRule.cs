using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Layout;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Text;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Líneas en blanco de C# y VB:
    /// 1.4 después de abrir un bloque, 1.5 antes de cerrarlo, 1.6 después de atributos, 1.7 entre llamadas encadenadas (se quitan);
    /// 3.1 entre miembros, 3.2 alrededor de regiones, 3.3 antes de cada case, 3.4 antes de comentarios, 3.5 después de usings (se asegura una).
    /// </summary>
    public class BlankLineLayoutRule : ISweepRule
    {
        public string Id => "BLANK_LINE_LAYOUT";
        public int Order => 40;

        public bool IsEnabled(SweepOptions options)
        {
            return options.EnableRemoveBlankLinesAfterOpenBlock
                || options.EnableRemoveBlankLinesBeforeCloseBlock
                || options.EnableRemoveBlankLinesAfterAttributes
                || options.EnableRemoveBlankLinesBetweenChainedCalls
                || options.EnableBlankLineBetweenMembers
                || options.EnableBlankLineAroundRegions
                || options.EnableBlankLineBeforeCase
                || options.EnableBlankLineBeforeSingleLineComments
                || options.EnableBlankLineAfterUsings;
        }

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot == null)
                return document;

            ILayoutSyntax? syntax = LayoutSyntax.For(document.Project.Language);
            if (syntax == null)
                return document;

            SourceText text = await document.GetTextAsync(cancellationToken);
            SyntaxTextGuard guard = new SyntaxTextGuard(syntaxRoot, SyntaxTextGuard.GetLines(text));
            LinePlan plan = new LinePlan(guard);
            LayoutAnalysis analysis = syntax.Analyze(syntaxRoot, text, MemberKinds.Parse(options.BlankLineBetweenMemberKinds));

            Apply(analysis, plan, options);

            IReadOnlyList<TextChange> changes = plan.Build();
            return changes.Count == 0 ? document : document.WithText(text.WithChanges(changes));
        }

        private static void Apply(LayoutAnalysis analysis, LinePlan plan, SweepOptions options)
        {
            if (options.EnableRemoveBlankLinesAfterOpenBlock)
            {
                analysis.OpenerLines.ForEach(plan.RemoveBlankLinesAfter);
            }

            if (options.EnableRemoveBlankLinesBeforeCloseBlock)
            {
                analysis.CloserLines.ForEach(plan.RemoveBlankLinesBefore);
            }

            if (options.EnableRemoveBlankLinesAfterAttributes)
            {
                analysis.AttributeEndLines.ForEach(plan.RemoveBlankLinesAfter);
            }

            if (options.EnableRemoveBlankLinesBetweenChainedCalls)
            {
                analysis.ChainedCallLines.ForEach(plan.RemoveBlankLinesBefore);
            }

            if (options.EnableBlankLineBetweenMembers)
            {
                analysis.MemberStartLines.ForEach(plan.EnsureBlankLineBefore);
            }

            if (options.EnableBlankLineAroundRegions)
            {
                foreach (int line in analysis.RegionLines)
                {
                    // Pegado a la llave / bloque que abre o cierra: ahí mandan 1.4 y 1.5.
                    if (!analysis.OpenerLines.Contains(line - 1))
                    {
                        plan.EnsureBlankLineBefore(line);
                    }

                    if (!analysis.CloserLines.Contains(line + 1))
                    {
                        plan.EnsureBlankLineAfter(line);
                    }
                }
            }

            if (options.EnableBlankLineBeforeCase)
            {
                analysis.CaseStartLines.ForEach(plan.EnsureBlankLineBefore);
            }

            if (options.EnableBlankLineBeforeSingleLineComments)
            {
                foreach (int line in analysis.CommentLines)
                {
                    if (CanPadComment(analysis, plan, line))
                    {
                        plan.EnsureBlankLineBefore(line);
                    }
                }
            }

            if (options.EnableBlankLineAfterUsings)
            {
                foreach (int line in analysis.LastUsingLines.Where(l => !analysis.CloserLines.Contains(l + 1)))
                {
                    plan.EnsureBlankLineAfter(line);
                }
            }
        }

        /// <summary>
        /// No se separa un comentario que va justo después de abrir un bloque, de un case, de otro comentario o de una directiva.
        /// </summary>
        private static bool CanPadComment(LayoutAnalysis analysis, LinePlan plan, int line)
        {
            int previous = line - 1;
            if (previous < 0 || plan.IsBlank(previous) || analysis.OpenerLines.Contains(previous))
                return false;

            string previousText = plan.GetText(previous).Trim();
            return !previousText.StartsWith("//")
                && !previousText.StartsWith("'")
                && !previousText.StartsWith("#")
                && !previousText.EndsWith(":");
        }
    }
}
