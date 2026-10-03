namespace YelcoBot.CodeSweep.Domain.Services.Whitespace
{
    /// <summary>Reemplazo de <see cref="Length"/> caracteres desde <see cref="Start"/> por <see cref="NewText"/>.</summary>
    public sealed class TextEdit
    {
        public TextEdit(int start, int length, string newText)
        {
            Start = start;
            Length = length;
            NewText = newText;
        }

        public int Start { get; }

        public int Length { get; }

        public string NewText { get; }
    }
}
