using System.Text.RegularExpressions;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Routing;

namespace YelcoBot.CodeSweep.Domain.Services.Whitespace
{
    /// <summary>
    /// Reglas de markup (Web Forms, Razor, HTML, XML / Config, XAML), después de Format Document:
    /// 1.8 quitar líneas en blanco justo dentro de una etiqueta y 7.2 quitar comentarios vacíos &lt;!-- --&gt;.
    /// Lo que protege el <see cref="ITextGuard"/> (&lt;pre&gt;, &lt;textarea&gt;, &lt;script&gt;, CDATA) no se toca.
    /// </summary>
    public static class MarkupCleaner
    {
        /// <summary>La línea termina con una etiqueta que abre (no se cierra sola ni es de cierre): &lt;div class="x"&gt;.</summary>
        private static readonly Regex EndsWithOpeningTag = new Regex(@"<(?<name>[A-Za-z][\w:.\-]*)(\s[^<>]*)?(?<!/)>\s*$", RegexOptions.Compiled);

        /// <summary>La línea empieza con una etiqueta de cierre: &lt;/div&gt;.</summary>
        private static readonly Regex StartsWithClosingTag = new Regex(@"^\s*</[A-Za-z]", RegexOptions.Compiled);

        private static readonly Regex EmptyComment = new Regex(@"<!--\s*-->", RegexOptions.Compiled);

        /// <summary>Elementos HTML sin cierre (&lt;br&gt;, &lt;input …&gt;): no abren un bloque.</summary>
        private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
        };

        public static bool Supports(string extension) => FileTypeGroups.Markup.Contains(extension ?? string.Empty);

        public static bool IsAnyEnabled(SweepOptions options) => options.EnableRemoveBlankLinesInsideTags || options.EnableRemoveEmptyComments;

        /// <summary>Cambios a aplicar, sin solaparse y ordenados por posición.</summary>
        public static IReadOnlyList<TextEdit> FindEdits(IReadOnlyList<TextLineInfo> lines, ITextGuard guard, SweepOptions options)
        {
            HashSet<int> removed = new HashSet<int>();
            List<TextEdit> edits = new List<TextEdit>();

            if (options.EnableRemoveEmptyComments)
            {
                FindEmptyComments(lines, guard, removed, edits);
            }

            if (options.EnableRemoveBlankLinesInsideTags)
            {
                FindBlankLinesInsideTags(lines, guard, removed);
            }

            edits.AddRange(removed.Select(i => new TextEdit(lines[i].Start, lines[i].LengthIncludingLineBreak, string.Empty)));
            return edits.OrderBy(e => e.Start).ToList();
        }

        /// <summary>Si el comentario está solo en su línea se quita la línea; si no, solo el comentario.</summary>
        private static void FindEmptyComments(IReadOnlyList<TextLineInfo> lines, ITextGuard guard, HashSet<int> removed, List<TextEdit> edits)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!guard.CanTrimLineEnd(i))
                    continue;

                MatchCollection matches = EmptyComment.Matches(lines[i].Text);
                if (matches.Count == 0)
                    continue;

                if (string.IsNullOrWhiteSpace(EmptyComment.Replace(lines[i].Text, string.Empty)))
                {
                    removed.Add(i);
                    continue;
                }

                foreach (Match match in matches)
                {
                    edits.Add(new TextEdit(lines[i].Start + match.Index, match.Length, string.Empty));
                }
            }
        }

        private static void FindBlankLinesInsideTags(IReadOnlyList<TextLineInfo> lines, ITextGuard guard, HashSet<int> removed)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (removed.Contains(i) || lines[i].IsBlank)
                    continue;

                if (OpensTag(lines[i].Text))
                {
                    for (int j = i + 1; j < lines.Count && (removed.Contains(j) || lines[j].IsBlank); j++)
                    {
                        if (lines[j].IsBlank && guard.CanRemoveLine(j))
                        {
                            removed.Add(j);
                        }
                    }
                }

                if (StartsWithClosingTag.IsMatch(lines[i].Text))
                {
                    for (int j = i - 1; j >= 0 && (removed.Contains(j) || lines[j].IsBlank); j--)
                    {
                        if (lines[j].IsBlank && guard.CanRemoveLine(j))
                        {
                            removed.Add(j);
                        }
                    }
                }
            }
        }

        private static bool OpensTag(string line)
        {
            Match match = EndsWithOpeningTag.Match(line);
            return match.Success && !VoidElements.Contains(match.Groups["name"].Value);
        }
    }
}
