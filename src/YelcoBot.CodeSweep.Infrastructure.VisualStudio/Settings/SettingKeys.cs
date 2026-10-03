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
        private const string Whitespace = "codeSweep.whitespace.";
        private const string CSharpVb = "codeSweep.csharpVb.";
        private const string Markup = "codeSweep.markup.";
        private const string Sql = "codeSweep.sql.";
        private const string FileTypes = "codeSweep.fileTypes.";

        public const string CleanupOnSave = General + "cleanupOnSave";

        public const string RemoveUnusedLocalVariables = Rules + "removeUnusedLocalVariables";
        public const string RemoveUnusedUsings = Rules + "removeUnusedUsings";
        public const string SortUsings = Rules + "sortUsings";
        public const string FormatDocument = Rules + "formatDocument";

        public const string RemoveConsecutiveBlankLines = Whitespace + "removeConsecutiveBlankLines";
        public const string MaxConsecutiveBlankLines = Whitespace + "maxConsecutiveBlankLines";
        public const string RemoveLeadingBlankLines = Whitespace + "removeLeadingBlankLines";
        public const string RemoveTrailingBlankLines = Whitespace + "removeTrailingBlankLines";
        public const string TrimTrailingWhitespace = Whitespace + "trimTrailingWhitespace";
        public const string SingleFinalNewline = Whitespace + "singleFinalNewline";

        public const string RemoveBlankLinesAfterOpenBlock = CSharpVb + "removeBlankLinesAfterOpenBlock";
        public const string RemoveBlankLinesBeforeCloseBlock = CSharpVb + "removeBlankLinesBeforeCloseBlock";
        public const string RemoveBlankLinesAfterAttributes = CSharpVb + "removeBlankLinesAfterAttributes";
        public const string RemoveBlankLinesBetweenChainedCalls = CSharpVb + "removeBlankLinesBetweenChainedCalls";
        public const string BlankLineBetweenMembers = CSharpVb + "blankLineBetweenMembers";
        public const string BlankLineBetweenMemberKinds = CSharpVb + "blankLineBetweenMemberKinds";
        public const string BlankLineAroundRegions = CSharpVb + "blankLineAroundRegions";
        public const string BlankLineBeforeCase = CSharpVb + "blankLineBeforeCase";
        public const string BlankLineBeforeSingleLineComments = CSharpVb + "blankLineBeforeSingleLineComments";
        public const string BlankLineAfterUsings = CSharpVb + "blankLineAfterUsings";
        public const string RemoveAllRegions = CSharpVb + "removeAllRegions";
        public const string UpdateEndRegionText = CSharpVb + "updateEndRegionText";
        public const string RemoveEmptyRegions = CSharpVb + "removeEmptyRegions";
        public const string ReorganizeMembers = CSharpVb + "reorganizeMembers";
        public const string MemberKindOrder = CSharpVb + "memberKindOrder";
        public const string AccessOrder = CSharpVb + "accessOrder";
        public const string UniformAccessors = CSharpVb + "uniformAccessors";
        public const string WrapComments = CSharpVb + "wrapComments";
        public const string CommentWrapColumn = CSharpVb + "commentWrapColumn";
        public const string SpaceAfterCommentPrefix = CSharpVb + "spaceAfterCommentPrefix";
        public const string RemoveBlankLinesInsideTags = Markup + "removeBlankLinesInsideTags";
        public const string RemoveEmptyComments = Markup + "removeEmptyComments";

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
        public const string IncludeSql = FileTypes + "includeSql";
        public const string FormatSql = Sql + "formatSql";

        /// <summary>codeSweep.sqlFormatter.formatting.keywordCasing (igual que en CodeSweep.registration.json).</summary>
        public static string SqlFormatter(Domain.Options.SqlFormatterOption option)
            => "codeSweep.sqlFormatter." + option.Group + "." + char.ToLowerInvariant(option.Name[0]) + option.Name.Substring(1);

        public const string AdditionalFileExtensions = FileTypes + "additionalFileExtensions";

        public const string IgnoreGeneratedCode = FileTypes + "ignoreGeneratedCode";
        public const string ExcludeT4GeneratedCode = FileTypes + "excludeT4GeneratedCode";
        public const string ExcludePatterns = FileTypes + "excludePatterns";
    }
}
