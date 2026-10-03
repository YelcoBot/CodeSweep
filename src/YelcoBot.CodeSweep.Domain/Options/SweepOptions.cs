namespace YelcoBot.CodeSweep.Domain.Options
{
    public class SweepOptions
    {
        public const string DefaultExcludePatterns = @"\.Designer\.cs$;\.Designer\.vb$;\.resx$;\.min\.css$;\.min\.js$";

        // A. Roslyn (C# y VB)
        public bool EnableRemoveUnusedUsings { get; set; } = true;
        public bool EnableSortUsings { get; set; } = true;
        public bool EnableRemoveUnusedLocalVariables { get; set; } = true;
        public bool EnableFormatDocument { get; set; } = true;

        // B. Universales (cualquier archivo, en las tres vías)
        public bool EnableRemoveConsecutiveBlankLines { get; set; } = true;

        /// <summary>Máximo de líneas en blanco seguidas que deja la regla 1.1.</summary>
        public int MaxConsecutiveBlankLines { get; set; } = 1;
        public bool EnableRemoveLeadingBlankLines { get; set; } = true;
        public bool EnableRemoveTrailingBlankLines { get; set; } = true;
        public bool EnableTrimTrailingWhitespace { get; set; } = true;
        public bool EnableSingleFinalNewline { get; set; } = true;

        // C.1 C# y VB
        public bool EnableRemoveBlankLinesAfterOpenBlock { get; set; } = true;
        public bool EnableRemoveBlankLinesBeforeCloseBlock { get; set; } = true;
        public bool EnableRemoveBlankLinesAfterAttributes { get; set; } = true;
        public bool EnableRemoveBlankLinesBetweenChainedCalls { get; set; } = true;
        public bool EnableBlankLineBetweenMembers { get; set; } = true;

        /// <summary>Tipos de miembro que llevan línea en blanco entre ellos (ver <see cref="MemberKinds"/>), separados por ';'.</summary>
        public string BlankLineBetweenMemberKinds { get; set; } = MemberKinds.DefaultBlankLineKinds;
        public bool EnableBlankLineAroundRegions { get; set; } = false;
        public bool EnableBlankLineBeforeCase { get; set; } = false;
        public bool EnableBlankLineBeforeSingleLineComments { get; set; } = true;
        public bool EnableBlankLineAfterUsings { get; set; } = true;
        public bool EnableRemoveAllRegions { get; set; } = false;
        public bool EnableUpdateEndRegionText { get; set; } = false;
        public bool EnableRemoveEmptyRegions { get; set; } = true;
        public bool EnableReorganizeMembers { get; set; } = false;

        /// <summary>Orden de los tipos de miembro al reorganizar (ver <see cref="MemberKinds"/>), separados por ';'.</summary>
        public string MemberKindOrder { get; set; } = MemberKinds.DefaultOrder;

        /// <summary>Orden de los accesos al reorganizar, separados por ';'.</summary>
        public string AccessOrder { get; set; } = MemberKinds.DefaultAccessOrder;
        public bool EnableUniformAccessors { get; set; } = false;
        public bool EnableWrapComments { get; set; } = false;
        public int CommentWrapColumn { get; set; } = 120;
        public bool EnableSpaceAfterCommentPrefix { get; set; } = false;

        // C.2 Markup (Web Forms, Razor, HTML, XML / Config, XAML)
        public bool EnableRemoveBlankLinesInsideTags { get; set; } = true;
        public bool EnableRemoveEmptyComments { get; set; } = true;

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
