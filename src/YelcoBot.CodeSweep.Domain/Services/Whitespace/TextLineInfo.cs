namespace YelcoBot.CodeSweep.Domain.Services.Whitespace
{
    /// <summary>Una línea del archivo: posición absoluta, texto sin el salto de línea y el salto de línea ("" en la última).</summary>
    public sealed class TextLineInfo
    {
        public TextLineInfo(int start, string text, string lineBreak)
        {
            Start = start;
            Text = text;
            LineBreak = lineBreak;
        }

        public int Start { get; }
        public string Text { get; }
        public string LineBreak { get; }

        public int LengthIncludingLineBreak => Text.Length + LineBreak.Length;
        public bool IsBlank => string.IsNullOrWhiteSpace(Text);
    }
}
