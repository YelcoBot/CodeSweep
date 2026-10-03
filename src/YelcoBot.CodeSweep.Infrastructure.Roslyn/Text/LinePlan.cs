using Microsoft.CodeAnalysis.Text;
using YelcoBot.CodeSweep.Domain.Services.Whitespace;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Text
{
    /// <summary>
    /// Junta cambios por línea (quitar líneas en blanco, asegurar una línea en blanco, quitar o reemplazar una línea)
    /// y los convierte en un solo conjunto de TextChange. Nunca toca lo que protege el <see cref="SyntaxTextGuard"/>.
    /// </summary>
    internal sealed class LinePlan
    {
        private readonly SyntaxTextGuard _guard;
        private readonly HashSet<int> _removeBlankLinesAfter = new HashSet<int>();
        private readonly HashSet<int> _removeBlankLinesBefore = new HashSet<int>();
        private readonly HashSet<int> _ensureBlankLineBefore = new HashSet<int>();
        private readonly HashSet<int> _removeLines = new HashSet<int>();
        private readonly Dictionary<int, string> _replaceLines = new Dictionary<int, string>();

        public LinePlan(SyntaxTextGuard guard)
        {
            _guard = guard;
        }

        private IReadOnlyList<TextLineInfo> Lines => _guard.Lines;

        public bool IsBlank(int line) => line >= 0 && line < Lines.Count && Lines[line].IsBlank;

        public string GetText(int line) => Lines[line].Text;

        /// <summary>Quita las líneas en blanco que siguen inmediatamente a la línea.</summary>
        public void RemoveBlankLinesAfter(int line) => _removeBlankLinesAfter.Add(line);

        /// <summary>Quita las líneas en blanco que preceden inmediatamente a la línea.</summary>
        public void RemoveBlankLinesBefore(int line) => _removeBlankLinesBefore.Add(line);

        /// <summary>Si la línea anterior no está en blanco, agrega una.</summary>
        public void EnsureBlankLineBefore(int line) => _ensureBlankLineBefore.Add(line);

        /// <summary>Si la línea siguiente no está en blanco, agrega una.</summary>
        public void EnsureBlankLineAfter(int line)
        {
            if (line + 1 < Lines.Count && !Lines[line + 1].IsBlank)
            {
                _ensureBlankLineBefore.Add(line + 1);
            }
        }

        public void RemoveLine(int line) => _removeLines.Add(line);

        /// <summary>Reemplaza el texto de la línea (sin su salto de línea).</summary>
        public void ReplaceLine(int line, string text) => _replaceLines[line] = text;

        public IReadOnlyList<TextChange> Build()
        {
            HashSet<int> removed = new HashSet<int>(_removeLines);

            foreach (int anchor in _removeBlankLinesAfter)
            {
                for (int j = anchor + 1; j < Lines.Count && Lines[j].IsBlank && _guard.CanRemoveLine(j); j++)
                {
                    removed.Add(j);
                }
            }

            foreach (int anchor in _removeBlankLinesBefore)
            {
                for (int j = anchor - 1; j >= 0 && Lines[j].IsBlank && _guard.CanRemoveLine(j); j--)
                {
                    removed.Add(j);
                }
            }

            List<TextChange> changes = new List<TextChange>();
            string lineBreak = Lines.FirstOrDefault(l => l.LineBreak.Length > 0)?.LineBreak ?? "\r\n";

            foreach (int line in _ensureBlankLineBefore.OrderBy(l => l))
            {
                if (line <= 0 || line >= Lines.Count || removed.Contains(line) || !_guard.CanInsertLineBefore(line))
                    continue;

                int previous = line - 1;
                while (previous >= 0 && removed.Contains(previous))
                {
                    previous--;
                }

                if (previous >= 0 && !Lines[previous].IsBlank)
                {
                    changes.Add(new TextChange(new TextSpan(Lines[line].Start, 0), lineBreak));
                }
            }

            foreach (int line in removed)
            {
                changes.Add(new TextChange(new TextSpan(Lines[line].Start, Lines[line].LengthIncludingLineBreak), string.Empty));
            }

            foreach (KeyValuePair<int, string> replacement in _replaceLines.Where(r => !removed.Contains(r.Key)))
            {
                TextLineInfo line = Lines[replacement.Key];
                if (line.Text != replacement.Value)
                {
                    changes.Add(new TextChange(new TextSpan(line.Start, line.Text.Length), replacement.Value));
                }
            }

            // Inserciones (largo 0) antes que el reemplazo que empieza en la misma posición.
            return changes.OrderBy(c => c.Span.Start).ThenBy(c => c.Span.Length).ToList();
        }
    }
}
