using System.Text.RegularExpressions;

namespace YelcoBot.CodeSweep.Domain.Services.Whitespace
{
    /// <summary>
    /// Protección para los archivos que formatea el editor de VS (aspx, html, xml, css, js…), por línea:
    /// - JS/TS: líneas dentro de template strings (`…`) o strings con continuación de línea.
    /// - SQL: líneas dentro de strings, identificadores "…" o […] de varias líneas.
    /// - Resto: bloques donde los saltos de línea son contenido: &lt;pre&gt;, &lt;textarea&gt;, &lt;script&gt; y CDATA.
    /// </summary>
    public sealed class EditorTextGuard : ITextGuard
    {
        private static readonly HashSet<string> ScriptExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".js", ".ts", ".jsx", ".tsx", ".mjs", ".cjs"
        };

        private static readonly (Regex Open, Regex Close)[] ProtectedBlocks =
        {
            (new Regex(@"<pre\b", RegexOptions.IgnoreCase), new Regex(@"</pre\s*>", RegexOptions.IgnoreCase)),
            (new Regex(@"<textarea\b", RegexOptions.IgnoreCase), new Regex(@"</textarea\s*>", RegexOptions.IgnoreCase)),
            (new Regex(@"<script\b", RegexOptions.IgnoreCase), new Regex(@"</script\s*>", RegexOptions.IgnoreCase)),
            (new Regex(@"<!\[CDATA\[", RegexOptions.IgnoreCase), new Regex(@"\]\]>", RegexOptions.IgnoreCase))
        };

        private readonly bool[] _protectedLines;

        private EditorTextGuard(bool[] protectedLines)
        {
            _protectedLines = protectedLines;
        }

        public static EditorTextGuard Create(string extension, IReadOnlyList<string> lines)
        {
            if (ScriptExtensions.Contains(extension ?? string.Empty))
                return new EditorTextGuard(ScriptScanner.FindProtectedLines(lines));

            if (string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase))
                return new EditorTextGuard(SqlScanner.FindProtectedLines(lines));

            return new EditorTextGuard(FindProtectedBlockLines(lines));
        }

        public bool CanRemoveLine(int lineIndex) => !_protectedLines[lineIndex];

        public bool CanTrimLineEnd(int lineIndex) => !_protectedLines[lineIndex];

        /// <summary>Desde la línea que abre el bloque hasta la que lo cierra, ambas incluidas.</summary>
        private static bool[] FindProtectedBlockLines(IReadOnlyList<string> lines)
        {
            bool[] result = new bool[lines.Count];
            int protectedBlock = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                if (protectedBlock >= 0)
                {
                    result[i] = true;
                    if (ProtectedBlocks[protectedBlock].Close.IsMatch(line))
                    {
                        protectedBlock = -1;
                    }

                    continue;
                }

                protectedBlock = FindOpenedBlock(line);
                result[i] = protectedBlock >= 0;
            }

            return result;
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
