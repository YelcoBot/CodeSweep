using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Text;
using CS = Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VB = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Regiones de C# (#region / #endregion) y VB (#Region / #End Region):
    /// 4.1 quitar todas (se conserva el contenido), 4.2 poner el nombre en el cierre, 4.3 quitar las vacías.
    /// Solo se tocan directivas que están solas en su línea.
    /// </summary>
    public class RegionsRule : ISweepRule
    {
        public string Id => "REGIONS";
        public int Order => 35;

        public bool IsEnabled(SweepOptions options)
            => options.EnableRemoveAllRegions || options.EnableUpdateEndRegionText || options.EnableRemoveEmptyRegions;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot == null || syntaxRoot.Language is not (LanguageNames.CSharp or LanguageNames.VisualBasic))
                return document;

            SourceText text = await document.GetTextAsync(cancellationToken);
            SyntaxTextGuard guard = new SyntaxTextGuard(syntaxRoot, SyntaxTextGuard.GetLines(text));
            LinePlan plan = new LinePlan(guard);

            List<RegionPair> regions = FindRegions(syntaxRoot, text, syntaxRoot.Language == LanguageNames.CSharp);
            if (regions.Count == 0)
                return document;

            if (options.EnableRemoveAllRegions)
            {
                foreach (RegionPair region in regions)
                {
                    plan.RemoveLine(region.StartLine);
                    plan.RemoveLine(region.EndLine);
                }
            }
            else
            {
                Apply(regions, plan, syntaxRoot.Language, options);
            }

            IReadOnlyList<TextChange> changes = plan.Build();
            return changes.Count == 0 ? document : document.WithText(text.WithChanges(changes));
        }

        private static void Apply(List<RegionPair> regions, LinePlan plan, string language, SweepOptions options)
        {
            HashSet<int> removedLines = new HashSet<int>();

            // Las regiones vienen de adentro hacia afuera: al quitar una vacía, la que la contiene puede quedar vacía.
            foreach (RegionPair region in regions)
            {
                bool isEmpty = Enumerable.Range(region.StartLine + 1, region.EndLine - region.StartLine - 1)
                    .All(line => removedLines.Contains(line) || plan.IsBlank(line));

                if (options.EnableRemoveEmptyRegions && isEmpty)
                {
                    for (int line = region.StartLine; line <= region.EndLine; line++)
                    {
                        removedLines.Add(line);
                        plan.RemoveLine(line);
                    }

                    continue;
                }

                if (options.EnableUpdateEndRegionText && region.Name.Length > 0)
                {
                    string endLine = plan.GetText(region.EndLine);
                    string indentation = endLine.Substring(0, endLine.Length - endLine.TrimStart().Length);

                    // VB no admite texto después de #End Region: el nombre va como comentario.
                    plan.ReplaceLine(region.EndLine, language == LanguageNames.CSharp
                        ? indentation + "#endregion " + region.Name
                        : indentation + "#End Region ' " + region.Name);
                }
            }
        }

        /// <summary>Pares #region / #endregion solos en su línea, de adentro hacia afuera.</summary>
        private static List<RegionPair> FindRegions(SyntaxNode root, SourceText text, bool isCSharp)
        {
            List<RegionPair> regions = new List<RegionPair>();
            Stack<(int Line, string Name)?> open = new Stack<(int Line, string Name)?>();

            foreach (SyntaxTrivia trivia in root.DescendantTrivia().Where(t => t.IsDirective))
            {
                int line = text.Lines.GetLineFromPosition(trivia.SpanStart).LineNumber;
                bool aloneOnLine = text.Lines[line].ToString().Trim() == trivia.ToString().Trim();

                if (IsRegionStart(trivia, isCSharp))
                {
                    open.Push(aloneOnLine ? (line, GetRegionName(trivia)) : null);
                }
                else if (IsRegionEnd(trivia, isCSharp) && open.Count > 0)
                {
                    (int Line, string Name)? start = open.Pop();
                    if (start.HasValue && aloneOnLine)
                    {
                        regions.Add(new RegionPair(start.Value.Line, line, start.Value.Name));
                    }
                }
            }

            return regions;
        }

        private static bool IsRegionStart(SyntaxTrivia trivia, bool isCSharp)
            => trivia.RawKind == (isCSharp ? (int)CSharpKind.RegionDirectiveTrivia : (int)VBKind.RegionDirectiveTrivia);

        private static bool IsRegionEnd(SyntaxTrivia trivia, bool isCSharp)
            => trivia.RawKind == (isCSharp ? (int)CSharpKind.EndRegionDirectiveTrivia : (int)VBKind.EndRegionDirectiveTrivia);

        private static string GetRegionName(SyntaxTrivia trivia)
        {
            return trivia.GetStructure() switch
            {
                CS.RegionDirectiveTriviaSyntax region => region.EndOfDirectiveToken.LeadingTrivia.ToString().Trim(),
                VB.RegionDirectiveTriviaSyntax region => region.Name.ValueText.Trim(),
                _ => string.Empty
            };
        }

        private readonly struct RegionPair
        {
            public RegionPair(int startLine, int endLine, string name)
            {
                StartLine = startLine;
                EndLine = endLine;
                Name = name;
            }

            public int StartLine { get; }
            public int EndLine { get; }
            public string Name { get; }
        }
    }
}
