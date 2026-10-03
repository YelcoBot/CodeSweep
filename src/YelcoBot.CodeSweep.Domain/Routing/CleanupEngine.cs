namespace YelcoBot.CodeSweep.Domain.Routing
{
    public enum CleanupEngine
    {
        Skip,
        Roslyn,
        Editor,

        /// <summary>T-SQL: se formatea con ScriptDOM en segundo plano, sin abrir editores.</summary>
        Sql
    }
}
