namespace YelcoBot.CodeSweep.Application.Abstractions
{
    /// <summary>Formatea T-SQL (archivos .sql) sin abrir editores.</summary>
    public interface ISqlFormatter
    {
        /// <summary>
        /// Devuelve false (y el motivo) si no es seguro formatear: errores de sintaxis, SQLCMD (:setvar, $(var))
        /// o el resultado perdería comentarios. En ese caso el archivo no se toca.
        /// </summary>
        /// <param name="settings">
        /// Propiedad de ScriptDOM → valor ("KeywordCasing" → "Uppercase"), ya resuelto: opciones del producto (VS / SSMS)
        /// y, para las que el producto no tiene, las de CodeSweep. El .editorconfig del archivo gana sobre estas.
        /// </param>
        bool TryFormat(string text, string filePath, IReadOnlyDictionary<string, string> settings, out string formatted, out string? reason);
    }
}
