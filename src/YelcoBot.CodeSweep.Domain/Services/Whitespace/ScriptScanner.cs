using System.Collections.Generic;
using System.Text;

namespace YelcoBot.CodeSweep.Domain.Services.Whitespace
{
    /// <summary>
    /// Recorre un archivo JS/TS para saber qué líneas están dentro de un template string (`…`, con ${…} anidados)
    /// o de un string con continuación de línea: ahí los espacios y las líneas en blanco son texto.
    /// Reconoce comentarios y literales de regex para no confundir sus comillas.
    /// Si el archivo no termina en un estado coherente, protege todo (no se toca nada).
    /// </summary>
    internal static class ScriptScanner
    {
        private enum State
        {
            Code,
            SingleQuote,
            DoubleQuote,
            Template,
            LineComment,
            BlockComment,
            Regex,
            RegexClass
        }

        /// <summary>Tokens después de los cuales "/" abre una regex (y no es una división).</summary>
        private static readonly HashSet<string> RegexPrefixes = new HashSet<string>
        {
            "(", ",", "=", ":", "[", "!", "&", "|", "?", "{", "}", ";", "+", "-", "*", "%", "<", ">", "~", "^",
            "return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw", "case", "do", "else", "yield", "await"
        };

        public static bool[] FindProtectedLines(IReadOnlyList<string> lines)
        {
            bool[] result = new bool[lines.Count];
            State state = State.Code;

            // Por cada ${ abierto dentro de un template: cuántas llaves de código hay abiertas dentro de él.
            List<int> templateBraces = new List<int>();
            StringBuilder word = new StringBuilder();
            string? lastToken = null;

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                string line = lines[lineIndex];
                bool startsInString = IsString(state);

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    char next = i + 1 < line.Length ? line[i + 1] : '\0';

                    switch (state)
                    {
                        case State.Code:
                            if (IsWordChar(c))
                            {
                                word.Append(c);
                                continue;
                            }

                            if (word.Length > 0)
                            {
                                lastToken = word.ToString();
                                word.Clear();
                            }

                            if (char.IsWhiteSpace(c))
                                continue;

                            if (c == '/' && next == '/')
                            {
                                state = State.LineComment;
                                i++;
                            }
                            else if (c == '/' && next == '*')
                            {
                                state = State.BlockComment;
                                i++;
                            }
                            else if (c == '/' && (lastToken == null || RegexPrefixes.Contains(lastToken)))
                            {
                                state = State.Regex;
                            }
                            else if (c == '\'')
                            {
                                state = State.SingleQuote;
                            }
                            else if (c == '"')
                            {
                                state = State.DoubleQuote;
                            }
                            else if (c == '`')
                            {
                                state = State.Template;
                            }
                            else if (c == '{' && templateBraces.Count > 0)
                            {
                                templateBraces[templateBraces.Count - 1]++;
                            }
                            else if (c == '}' && templateBraces.Count > 0)
                            {
                                int last = templateBraces.Count - 1;
                                if (templateBraces[last] == 0)
                                {
                                    templateBraces.RemoveAt(last);
                                    state = State.Template;
                                }
                                else
                                {
                                    templateBraces[last]--;
                                }
                            }

                            // Un string, template o regex funciona como un valor: un "/" después es una división.
                            // Los comentarios no cuentan como token.
                            if (state is not (State.LineComment or State.BlockComment))
                            {
                                lastToken = state is State.Code ? c.ToString() : "value";
                            }

                            break;

                        case State.SingleQuote:
                        case State.DoubleQuote:
                            if (c == '\\')
                            {
                                i++;
                            }
                            else if (c == (state == State.SingleQuote ? '\'' : '"'))
                            {
                                state = State.Code;
                            }

                            break;

                        case State.Template:
                            if (c == '\\')
                            {
                                i++;
                            }
                            else if (c == '`')
                            {
                                state = State.Code;
                            }
                            else if (c == '$' && next == '{')
                            {
                                templateBraces.Add(0);
                                state = State.Code;
                                lastToken = "{";
                                i++;
                            }

                            break;

                        case State.BlockComment:
                            if (c == '*' && next == '/')
                            {
                                state = State.Code;
                                i++;
                            }

                            break;

                        case State.Regex:
                            if (c == '\\')
                            {
                                i++;
                            }
                            else if (c == '[')
                            {
                                state = State.RegexClass;
                            }
                            else if (c == '/')
                            {
                                state = State.Code;
                            }

                            break;

                        case State.RegexClass:
                            if (c == '\\')
                            {
                                i++;
                            }
                            else if (c == ']')
                            {
                                state = State.Regex;
                            }

                            break;
                    }
                }

                if (word.Length > 0)
                {
                    lastToken = word.ToString();
                    word.Clear();
                }

                bool endsInString = IsString(state) && (state == State.Template || EndsWithLineContinuation(line));
                result[lineIndex] = startsInString || endsInString;

                // Al final de la línea: el comentario de línea termina; un string o regex sin cerrar es un error de sintaxis.
                if (state is State.LineComment or State.Regex or State.RegexClass
                    || (state is State.SingleQuote or State.DoubleQuote && !endsInString))
                {
                    state = State.Code;
                }
            }

            if (state != State.Code || templateBraces.Count > 0)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = true;
                }
            }

            return result;
        }

        private static bool IsString(State state) => state is State.SingleQuote or State.DoubleQuote or State.Template;

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '$';

        /// <summary>Termina en una cantidad impar de "\" (la última escapa el salto de línea).</summary>
        private static bool EndsWithLineContinuation(string line)
        {
            int backslashes = 0;
            for (int i = line.Length - 1; i >= 0 && line[i] == '\\'; i--)
            {
                backslashes++;
            }

            return backslashes % 2 == 1;
        }
    }
}
