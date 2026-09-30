namespace YelcoBot.CodeSweep.Domain.Options
{
    public class SweepOptions
    {
        public const string DefaultExcludePatterns = @"\.Designer\.cs$;\.Designer\.vb$;\.resx$;\.min\.css$;\.min\.js$";

        // Reglas (C#)
        public bool EnableRemoveUnusedUsings { get; set; } = true;
        public bool EnableSortUsings { get; set; } = true;
        public bool EnableRemoveUnusedLocalVariables { get; set; } = true;
        public bool EnableRemoveConsecutiveBlankLines { get; set; } = true;
        public bool EnableFormatDocument { get; set; } = true;

        // Tipos de archivo: Roslyn
        public bool IncludeCSharp { get; set; } = true;
        public bool IncludeVisualBasic { get; set; } = true;

        // Tipos de archivo: editor de VS (ver FileTypeGroups)
        public bool IncludeWebForms { get; set; } = true;

        /// <summary>
        /// Tras formatear .aspx/.ascx/.master, regenerar sus .designer.cs con "Convert to Web Application".
        /// Apagado por defecto: formatear no cambia controles, así que el designer no debería cambiar.
        /// </summary>
        public bool RegenerateWebFormsDesigner { get; set; } = false;
        public bool IncludeRazor { get; set; } = true;
        public bool IncludeHtml { get; set; } = true;
        public bool IncludeXmlConfig { get; set; } = true;
        public bool IncludeXaml { get; set; } = true;
        public bool IncludeStyles { get; set; } = true;
        public bool IncludeScripts { get; set; } = true;
        public bool IncludeJson { get; set; } = true;

        /// <summary>Extensiones extra que van al editor de VS, separadas por ';' (ej. ".skin;.sitemap").</summary>
        public string AdditionalFileExtensions { get; set; } = string.Empty;

        // Exclusiones
        public bool IgnoreGeneratedCode { get; set; } = true;
        public bool ExcludeT4GeneratedCode { get; set; } = true;

        /// <summary>Expresiones regulares sobre la ruta completa, separadas por ';'.</summary>
        public string ExcludePatterns { get; set; } = DefaultExcludePatterns;

        // Automatización
        public bool CleanupOnSave { get; set; } = false;

        /// <summary>Interruptor general de la fase del editor (todos los grupos no-Roslyn).</summary>
        public bool FormatEditorFiles { get; set; } = true;
    }
}
