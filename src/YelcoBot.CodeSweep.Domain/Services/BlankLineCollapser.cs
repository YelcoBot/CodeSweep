using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace YelcoBot.CodeSweep.Domain.Services
{
    /// <summary>
    /// Decide qué líneas en blanco sobran en archivos que no son C# (aspx, html, xml, css…):
    /// deja como máximo <see cref="MaxConsecutiveBlankLines"/> seguidas.
    /// No toca bloques donde los saltos de línea son contenido: &lt;pre&gt;, &lt;textarea&gt;, &lt;script&gt; y CDATA.
    /// </summary>
    public static class BlankLineCollapser
    {
        public const int MaxConsecutiveBlankLines = 1;

        /// <summary>JS/TS: los template strings (`…`) pueden tener líneas en blanco que son texto. No se tocan.</summary>
        private static readonly HashSet<string> UnsafeExtensions = new(StringComparer.OrdinalIgnoreCase) { ".js", ".ts" };

        private static readonly (Regex Open, Regex Close)[] ProtectedBlocks =
        {
            (new Regex(@"<pre\b", RegexOptions.IgnoreCase), new Regex(@"</pre\s*>", RegexOptions.IgnoreCase)),
            (new Regex(@"<textarea\b", RegexOptions.IgnoreCase), new Regex(@"</textarea\s*>", RegexOptions.IgnoreCase)),
            (new Regex(@"<script\b", RegexOptions.IgnoreCase), new Regex(@"</script\s*>", RegexOptions.IgnoreCase)),
            (new Regex(@"<!\[CDATA\[", RegexOptions.IgnoreCase), new Regex(@"\]\]>", RegexOptions.IgnoreCase))
        };

        public static bool Supports(string extension) => !UnsafeExtensions.Contains(extension ?? string.Empty);

        /// <summary>Índices (0-based) de las líneas en blanco a eliminar, de menor a mayor.</summary>
        public static IReadOnlyList<int> FindLinesToRemove(IReadOnlyList<string> lines)
        {
            List<int> toRemove = new List<int>();
            int blankRun = 0;
            int protectedBlock = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                if (protectedBlock >= 0)
                {
                    blankRun = 0;
                    if (ProtectedBlocks[protectedBlock].Close.IsMatch(line))
                    {
                        protectedBlock = -1;
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    blankRun++;
                    if (blankRun > MaxConsecutiveBlankLines)
                    {
                        toRemove.Add(i);
                    }

                    continue;
                }

                blankRun = 0;
                protectedBlock = FindOpenedBlock(line);
            }

            return toRemove;
        }

        /// <summary>Bloque protegido que se abre en esta línea y no se cierra en ella (-1 si ninguno).</summary>
        private static int FindOpenedBlock(string line)
        {
            for (int b = 0; b < ProtectedBlocks.Length; b++)
            {
                Match open = ProtectedBlocks[b].Open.Match(line);
                if (open.Success && !ProtectedBlocks[b].Close.IsMatch(line.Substring(open.Index)))
                {
                    return b;
                }
            }

            return -1;
        }
    }
}
