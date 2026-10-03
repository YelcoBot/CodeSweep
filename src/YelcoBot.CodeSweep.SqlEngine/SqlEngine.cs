using System.Reflection;
using EditorConfig.Core;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace YelcoBot.CodeSweep.SqlEngine
{
    /// <summary>Resultado de formatear (viaja entre AppDomains: solo tipos serializables).</summary>
    [Serializable]
    public sealed class SqlFormatResult
    {
        public bool Success { get; set; }

        public string Text { get; set; } = string.Empty;

        /// <summary>Error de sintaxis (línea, columna, mensaje de ScriptDOM en el idioma de la interfaz).</summary>
        public int ErrorLine { get; set; }

        public int ErrorColumn { get; set; }

        public string? ErrorMessage { get; set; }

        /// <summary>El resultado se descartó: perdería comentarios o no sería T-SQL válido.</summary>
        public bool Discarded { get; set; }
    }

    /// <summary>
    /// Lo que ve el AppDomain principal: solo tipos simples. Así ese dominio nunca carga ScriptDOM ni EditorConfig
    /// (en SSMS cargaría el ScriptDOM de SSMS, con la misma identidad).
    /// </summary>
    public interface ISqlEngine
    {
        SqlFormatResult Format(string text, string filePath, Dictionary<string, string> settings);
    }

    /// <summary>
    /// Motor de formato T-SQL: ScriptDOM (el mismo motor del "Format SQL" de SSMS) + .editorconfig.
    /// Se crea en un AppDomain aparte (ver SqlEngineDomain en Infrastructure.Sql).
    /// Prioridad de cada opción: .editorconfig [*.sql] → <c>settings</c> (opciones del producto o de CodeSweep, ya resueltas).
    /// </summary>
    public sealed class SqlEngine : MarshalByRefObject, ISqlEngine
    {
        private const SqlVersion DefaultVersion = SqlVersion.Sql170;

        /// <summary>Clave normalizada (sin "_", minúsculas) → propiedad: keyword_casing / KeywordCasing → "keywordcasing".</summary>
        private static readonly Dictionary<string, PropertyInfo> Properties = typeof(SqlScriptGeneratorOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => Normalize(p.Name));

        /// <summary>Vive mientras viva el AppDomain (sin esto, el proxy remoto caduca a los 5 minutos sin uso).</summary>
        public override object? InitializeLifetimeService() => null;

        /// <param name="settings">Propiedad de ScriptDOM → valor ("KeywordCasing" → "Uppercase"), ya resuelto por CodeSweep.</param>
        public SqlFormatResult Format(string text, string filePath, Dictionary<string, string> settings)
        {
            Dictionary<string, string> values = settings.ToDictionary(s => Normalize(s.Key), s => s.Value);

            // 1. .editorconfig gana sobre todo lo demás.
            foreach (KeyValuePair<string, string> property in ReadEditorConfig(filePath))
            {
                values[Normalize(property.Key)] = property.Value;
            }

            SqlScriptGeneratorOptions options = new SqlScriptGeneratorOptions();
            foreach (KeyValuePair<string, string> value in values)
            {
                Set(options, value.Key, value.Value);
            }

            SqlVersion version = options.SqlVersion;
            TSqlFragment fragment = TSqlParser.CreateParser(version, true).Parse(new StringReader(text), out IList<ParseError> errors);

            if (errors.Count > 0)
            {
                return new SqlFormatResult { ErrorLine = errors[0].Line, ErrorColumn = errors[0].Column, ErrorMessage = errors[0].Message };
            }

            CreateGenerator(version, options).GenerateScript(fragment, out string script);

            // Seguridad: el resultado debe ser T-SQL válido y conservar todos los comentarios.
            TSqlFragment check = TSqlParser.CreateParser(version, true).Parse(new StringReader(script), out IList<ParseError> checkErrors);
            if (checkErrors.Count > 0 || CountComments(check) != CountComments(fragment))
            {
                return new SqlFormatResult { Discarded = true };
            }

            return new SqlFormatResult { Success = true, Text = NormalizeLineBreaks(script, DetectLineBreak(text)) };
        }

        private IReadOnlyDictionary<string, string> ReadEditorConfig(string filePath)
        {
            try
            {
                // Una ventana de consulta de SSMS sin guardar puede no tener ruta completa: no hay .editorconfig que leer.
                if (!Path.IsPathRooted(filePath))
                    return new Dictionary<string, string>();

                // Un parser por archivo: su caché no debe esconder un .editorconfig creado o cambiado durante la sesión.
                return new EditorConfigParser().Parse(new[] { filePath }).FirstOrDefault()?.Properties ?? new Dictionary<string, string>();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>Asigna la opción si existe y el valor es válido; si no, la ignora (queda el valor de la fuente anterior).</summary>
        private static void Set(SqlScriptGeneratorOptions options, string key, string value)
        {
            if (!Properties.TryGetValue(key, out PropertyInfo? property))
                return;

            value = value.Trim();

            if (property.PropertyType == typeof(bool) && bool.TryParse(value, out bool boolValue))
            {
                property.SetValue(options, boolValue);
            }
            else if (property.PropertyType == typeof(int) && int.TryParse(value, out int intValue))
            {
                property.SetValue(options, intValue);
            }
            else if (property.PropertyType.IsEnum && TryParseEnum(property.PropertyType, value, out object? enumValue))
            {
                property.SetValue(options, enumValue);
            }
        }

        private static bool TryParseEnum(Type enumType, string value, out object? result)
        {
            string? name = Enum.GetNames(enumType).FirstOrDefault(n => Normalize(n) == Normalize(value));
            result = name == null ? null : Enum.Parse(enumType, name);
            return result != null;
        }

        /// <summary>Sql170 → Sql170ScriptGenerator (el generador de la misma versión que el parser).</summary>
        private static SqlScriptGenerator CreateGenerator(SqlVersion version, SqlScriptGeneratorOptions options)
        {
            Type? generatorType = typeof(SqlScriptGenerator).Assembly.GetType($"Microsoft.SqlServer.TransactSql.ScriptDom.{version}ScriptGenerator");
            if (generatorType != null && Activator.CreateInstance(generatorType, options) is SqlScriptGenerator generator)
                return generator;

            options.SqlVersion = DefaultVersion;
            return new Sql170ScriptGenerator(options);
        }

        private static int CountComments(TSqlFragment fragment)
        {
            return fragment.ScriptTokenStream?.Count(t => t.TokenType is TSqlTokenType.SingleLineComment or TSqlTokenType.MultilineComment) ?? 0;
        }

        private static string Normalize(string key) => key.Replace("_", string.Empty).ToLowerInvariant();

        private static string DetectLineBreak(string text)
        {
            int index = text.IndexOf('\n');
            return index > 0 && text[index - 1] == '\r' ? "\r\n" : index >= 0 ? "\n" : "\r\n";
        }

        private static string NormalizeLineBreaks(string text, string lineBreak)
        {
            return text.Replace("\r\n", "\n").Replace("\n", lineBreak);
        }
    }
}
