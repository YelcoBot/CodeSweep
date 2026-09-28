namespace YelcoBot.CodeSweep.Domain.Options
{
    public class SweepOptions
    {
        public const string DefaultEditorFileExtensions =
            ".aspx;.ascx;.master;.razor;.cshtml;.html;.htm;.css;.js;.ts;.json;.xml;.xaml;.config";

        public bool EnableRemoveUnusedUsings { get; set; } = true;
        public bool EnableSortUsings { get; set; } = true;
        public bool EnableRemoveUnusedLocalVariables { get; set; } = true;
        public bool EnableRemoveConsecutiveBlankLines { get; set; } = true;
        public bool EnableFormatDocument { get; set; } = true;

        public bool FormatEditorFiles { get; set; } = true;
        public string EditorFileExtensions { get; set; } = DefaultEditorFileExtensions;

        public bool CleanupOnSave { get; set; } = false;
        public bool IgnoreGeneratedCode { get; set; } = true;
    }
}
