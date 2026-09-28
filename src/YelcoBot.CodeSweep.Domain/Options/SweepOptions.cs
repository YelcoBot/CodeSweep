namespace YelcoBot.CodeSweep.Domain.Options
{
    public class SweepOptions
    {
        public bool EnableRemoveUnusedUsings { get; set; } = true;
        public bool EnableSortUsings { get; set; } = true;
        public bool EnableRemoveUnusedLocalVariables { get; set; } = true;
        public bool EnableRemoveConsecutiveBlankLines { get; set; } = true;
        public bool EnableFormatDocument { get; set; } = true;

        public bool CleanupOnSave { get; set; } = false;
        public bool IgnoreGeneratedCode { get; set; } = true;
    }
}
