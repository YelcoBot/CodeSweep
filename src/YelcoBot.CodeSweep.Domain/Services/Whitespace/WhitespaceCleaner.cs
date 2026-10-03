using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Domain.Services.Whitespace
{
    /// <summary>
    /// Reglas universales (cualquier archivo, en las tres vías: Roslyn, editor invisible y editor visible):
    /// 1.1 líneas en blanco consecutivas, 1.2 al inicio, 1.3 al final, 2.1 espacios al final de línea
    /// y 2.2 exactamente un salto de línea al final.
    /// Solo calcula los cambios; cada vía los aplica sobre su propio texto (SourceText de Roslyn o buffer del editor).
    /// Lo que el <see cref="ITextGuard"/> protege (strings, código deshabilitado, &lt;pre&gt;…) nunca se toca.
    /// </summary>
    public static class WhitespaceCleaner
    {
        public static bool IsAnyEnabled(SweepOptions options)
        {
            return options.EnableRemoveConsecutiveBlankLines
                || options.EnableRemoveLeadingBlankLines
                || options.EnableRemoveTrailingBlankLines
                || options.EnableTrimTrailingWhitespace
                || options.EnableSingleFinalNewline;
        }

        /// <summary>Cambios a aplicar, sin solaparse y ordenados por posición.</summary>
        public static IReadOnlyList<TextEdit> FindEdits(IReadOnlyList<TextLineInfo> lines, ITextGuard guard, SweepOptions options)
        {
            List<TextEdit> edits = new List<TextEdit>();
            if (lines.Count == 0 || !IsAnyEnabled(options))
                return edits;

            bool[] removed = new bool[lines.Count];
            int firstContent = FindFirstContentLine(lines);

            // Archivo vacío o solo líneas en blanco: no hay nada que ordenar.
            if (firstContent < 0)
                return edits;

            int lastContent = FindLastContentLine(lines);

            if (options.EnableRemoveLeadingBlankLines)
            {
                MarkLeadingBlankLines(guard, removed, firstContent);
            }

            if (options.EnableRemoveConsecutiveBlankLines)
            {
                MarkConsecutiveBlankLines(lines, guard, removed, firstContent, lastContent, Math.Max(0, options.MaxConsecutiveBlankLines));
            }

            // Lo que borra el final del archivo (1.3 / 2.2) ya no se recorta línea por línea.
            int endOfFileDeletionStart = AddEndOfFileEdits(lines, guard, lastContent, options, edits);

            for (int i = 0; i < lines.Count && lines[i].Start < endOfFileDeletionStart; i++)
            {
                if (removed[i])
                {
                    edits.Add(new TextEdit(lines[i].Start, lines[i].LengthIncludingLineBreak, string.Empty));
                }
                else if (options.EnableTrimTrailingWhitespace)
                {
                    AddTrimEdit(lines[i], i, guard, edits);
                }
            }

            return edits.OrderBy(e => e.Start).ThenBy(e => e.Length == 0 ? 1 : 0).ToList();
        }

        private static void MarkLeadingBlankLines(ITextGuard guard, bool[] removed, int firstContent)
        {
            for (int i = 0; i < firstContent && guard.CanRemoveLine(i); i++)
            {
                removed[i] = true;
            }
        }

        private static void MarkConsecutiveBlankLines(IReadOnlyList<TextLineInfo> lines, ITextGuard guard, bool[] removed, int firstContent, int lastContent, int maxBlankLines)
        {
            int blankRun = 0;
            for (int i = firstContent; i < lastContent; i++)
            {
                if (!lines[i].IsBlank || !guard.CanRemoveLine(i))
                {
                    blankRun = 0;
                    continue;
                }

                blankRun++;
                if (blankRun > maxBlankLines)
                {
                    removed[i] = true;
                }
            }
        }

        private static void AddTrimEdit(TextLineInfo line, int index, ITextGuard guard, List<TextEdit> edits)
        {
            int trimmedLength = line.Text.TrimEnd().Length;
            if (trimmedLength == line.Text.Length || !guard.CanTrimLineEnd(index))
                return;

            edits.Add(new TextEdit(line.Start + trimmedLength, line.Text.Length - trimmedLength, string.Empty));
        }

        /// <summary>
        /// 1.3: quita las líneas en blanco del final (se conserva el salto de línea de la última línea con contenido).
        /// 2.2: lo mismo, y además agrega el salto de línea final si falta.
        /// Devuelve dónde empieza lo borrado (int.MaxValue si no se borra nada).
        /// </summary>
        private static int AddEndOfFileEdits(IReadOnlyList<TextLineInfo> lines, ITextGuard guard, int lastContent, SweepOptions options, List<TextEdit> edits)
        {
            if (!options.EnableRemoveTrailingBlankLines && !options.EnableSingleFinalNewline)
                return int.MaxValue;

            TextLineInfo last = lines[lastContent];

            if (last.LineBreak.Length == 0)
            {
                if (options.EnableSingleFinalNewline && guard.CanTrimLineEnd(lastContent))
                {
                    edits.Add(new TextEdit(last.Start + last.Text.Length, 0, GetPreferredLineBreak(lines)));
                }

                return int.MaxValue;
            }

            for (int i = lastContent + 1; i < lines.Count; i++)
            {
                if (!guard.CanRemoveLine(i))
                    return int.MaxValue;
            }

            TextLineInfo end = lines[lines.Count - 1];
            int start = last.Start + last.LengthIncludingLineBreak;
            int length = end.Start + end.LengthIncludingLineBreak - start;

            if (length <= 0)
                return int.MaxValue;

            edits.Add(new TextEdit(start, length, string.Empty));
            return start;
        }

        private static int FindFirstContentLine(IReadOnlyList<TextLineInfo> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].IsBlank)
                    return i;
            }

            return -1;
        }

        private static int FindLastContentLine(IReadOnlyList<TextLineInfo> lines)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (!lines[i].IsBlank)
                    return i;
            }

            return -1;
        }

        /// <summary>El salto de línea que ya usa el archivo (CRLF si no tiene ninguno).</summary>
        private static string GetPreferredLineBreak(IReadOnlyList<TextLineInfo> lines)
        {
            return lines.FirstOrDefault(l => l.LineBreak.Length > 0)?.LineBreak ?? "\r\n";
        }
    }
}
