namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Claves de Unified Settings: deben coincidir con las propiedades de CodeSweep.registration.json
    /// (son las que VS escribe en settings.json).
    /// </summary>
    internal static class SettingKeys
    {
        private const string General = "codeSweep.general.";
        private const string Rules = "codeSweep.rules.";
        private const string FileTypes = "codeSweep.fileTypes.";

        public const string CleanupOnSave = General + "cleanupOnSave";

        public const string RemoveUnusedLocalVariables = Rules + "removeUnusedLocalVariables";
        public const string RemoveUnusedUsings = Rules + "removeUnusedUsings";
        public const string SortUsings = Rules + "sortUsings";
        public const string RemoveConsecutiveBlankLines = Rules + "removeConsecutiveBlankLines";
        public const string FormatDocument = Rules + "formatDocument";

        public const string IncludeCSharp = FileTypes + "includeCSharp";
        public const string IncludeVisualBasic = FileTypes + "includeVisualBasic";
        public const string IncludeWebForms = FileTypes + "includeWebForms";
        public const string RegenerateWebFormsDesigner = FileTypes + "regenerateWebFormsDesigner";
        public const string IncludeRazor = FileTypes + "includeRazor";
        public const string IncludeHtml = FileTypes + "includeHtml";
        public const string IncludeXmlConfig = FileTypes + "includeXmlConfig";
        public const string IncludeXaml = FileTypes + "includeXaml";
        public const string IncludeStyles = FileTypes + "includeStyles";
        public const string IncludeScripts = FileTypes + "includeScripts";
        public const string IncludeJson = FileTypes + "includeJson";
        public const string AdditionalFileExtensions = FileTypes + "additionalFileExtensions";

        public const string IgnoreGeneratedCode = FileTypes + "ignoreGeneratedCode";
        public const string ExcludeT4GeneratedCode = FileTypes + "excludeT4GeneratedCode";
        public const string ExcludePatterns = FileTypes + "excludePatterns";
    }
}
