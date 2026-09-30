namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Claves en el SettingsManager de VS. Deben coincidir con los "path" de "migration" en CodeSweep.registration.json.
    /// </summary>
    internal static class SettingKeys
    {
        private const string General = "CodeSweep.General.";
        private const string Rules = "CodeSweep.Rules.";
        private const string FileTypes = "CodeSweep.FileTypes.";

        public const string CleanupOnSave = General + "CleanupOnSave";

        public const string RemoveUnusedLocalVariables = Rules + "RemoveUnusedLocalVariables";
        public const string RemoveUnusedUsings = Rules + "RemoveUnusedUsings";
        public const string SortUsings = Rules + "SortUsings";
        public const string RemoveConsecutiveBlankLines = Rules + "RemoveConsecutiveBlankLines";
        public const string FormatDocument = Rules + "FormatDocument";

        public const string IncludeCSharp = FileTypes + "IncludeCSharp";
        public const string IncludeVisualBasic = FileTypes + "IncludeVisualBasic";
        public const string IncludeWebForms = FileTypes + "IncludeWebForms";
        public const string RegenerateWebFormsDesigner = FileTypes + "RegenerateWebFormsDesigner";
        public const string IncludeRazor = FileTypes + "IncludeRazor";
        public const string IncludeHtml = FileTypes + "IncludeHtml";
        public const string IncludeXmlConfig = FileTypes + "IncludeXmlConfig";
        public const string IncludeXaml = FileTypes + "IncludeXaml";
        public const string IncludeStyles = FileTypes + "IncludeStyles";
        public const string IncludeScripts = FileTypes + "IncludeScripts";
        public const string IncludeJson = FileTypes + "IncludeJson";
        public const string AdditionalFileExtensions = FileTypes + "AdditionalFileExtensions";

        public const string IgnoreGeneratedCode = FileTypes + "IgnoreGeneratedCode";
        public const string ExcludeT4GeneratedCode = FileTypes + "ExcludeT4GeneratedCode";
        public const string ExcludePatterns = FileTypes + "ExcludePatterns";
    }
}
