namespace YelcoBot.CodeSweep.Domain.Services.Whitespace
{
    /// <summary>
    /// Protege el contenido donde los espacios y saltos de línea son texto: strings multilínea, template strings,
    /// código deshabilitado, &lt;pre&gt;, &lt;textarea&gt;, &lt;script&gt;, CDATA…
    /// </summary>
    public interface ITextGuard
    {
        /// <summary>La línea (en blanco) se puede borrar completa.</summary>
        bool CanRemoveLine(int lineIndex);

        /// <summary>Los espacios al final de la línea se pueden quitar.</summary>
        bool CanTrimLineEnd(int lineIndex);
    }
}
