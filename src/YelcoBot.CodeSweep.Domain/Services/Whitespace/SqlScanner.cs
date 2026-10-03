namespace YelcoBot.CodeSweep.Domain.Services.Whitespace
{
    /// <summary>
    /// Recorre un archivo T-SQL para saber qué líneas están dentro de un string ('…', N'…'), de un identificador
    /// entre comillas ("…") o entre corchetes ([…]) que ocupan varias líneas: ahí los espacios y las líneas en blanco son texto.
    /// Reconoce comentarios (-- y /* */ anidados) para no confundir sus comillas.
    /// Si el archivo no termina en un estado coherente, protege todo (no se toca nada).
    /// </summary>
    internal static class SqlScanner
    {
        private enum State
        {
            Code,
            String,
            QuotedIdentifier,
            BracketIdentifier,
            BlockComment
        }

        public static bool[] FindProtectedLines(IReadOnlyList<string> lines)
        {
            bool[] result = new bool[lines.Count];
            State state = State.Code;
            int commentDepth = 0;

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                string line = lines[lineIndex];
                bool startsInText = IsText(state);

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    char next = i + 1 < line.Length ? line[i + 1] : '\0';

                    switch (state)
                    {
                        case State.Code:
                            if (c == '-' && next == '-')
                            {
                                i = line.Length;
                            }
                            else if (c == '/' && next == '*')
                            {
                                state = State.BlockComment;
                                commentDepth = 1;
                                i++;
                            }
                            else if (c == '\'')
                            {
                                state = State.String;
                            }
                            else if (c == '"')
                            {
                                state = State.QuotedIdentifier;
                            }
                            else if (c == '[')
                            {
                                state = State.BracketIdentifier;
                            }

                            break;

                        // '' dentro de un string, "" y ]] dentro de identificadores son el carácter escapado.
                        case State.String:
                            state = CloseOrEscape(c, next, '\'', ref i) ? State.Code : state;
                            break;

                        case State.QuotedIdentifier:
                            state = CloseOrEscape(c, next, '"', ref i) ? State.Code : state;
                            break;

                        case State.BracketIdentifier:
                            state = CloseOrEscape(c, next, ']', ref i) ? State.Code : state;
                            break;

                        // En T-SQL los comentarios /* */ se pueden anidar.
                        case State.BlockComment:
                            if (c == '/' && next == '*')
                            {
                                commentDepth++;
                                i++;
                            }
                            else if (c == '*' && next == '/')
                            {
                                commentDepth--;
                                i++;
                                if (commentDepth == 0)
                                {
                                    state = State.Code;
                                }
                            }

                            break;
                    }
                }

                result[lineIndex] = startsInText || IsText(state);
            }

            if (state != State.Code)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = true;
                }
            }

            return result;
        }

        private static bool IsText(State state) => state is State.String or State.QuotedIdentifier or State.BracketIdentifier;

        /// <summary>true si el carácter cierra; si está duplicado es un escape y se salta.</summary>
        private static bool CloseOrEscape(char c, char next, char closing, ref int i)
        {
            if (c != closing)
                return false;

            if (next == closing)
            {
                i++;
                return false;
            }

            return true;
        }
    }
}
