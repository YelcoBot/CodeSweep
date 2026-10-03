using Microsoft.VisualStudio.Shell;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Qué opciones del formateador T-SQL muestra CodeSweep en Tools → Options:
    /// solo las que el producto no tiene. Si el producto las tiene todas, no se muestra ninguna; si no tiene ninguna (VS), todas.
    /// </summary>
    public static class SqlFormatterVisibility
    {
        /// <summary>Activa el UIContext de cada opción que el producto ya tiene (CodeSweep.registration.json la oculta).</summary>
        public static async Task ApplyAsync()
        {
            IReadOnlyDictionary<string, HostSetting> hostSettings = await HostSqlFormatterSettings.Shared.GetAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            foreach (string option in hostSettings.Keys)
            {
                if (SqlFormatterUIContexts.ByOption.TryGetValue(option, out Guid context))
                {
                    UIContext.FromUIContextGuid(context).IsActive = true;
                }
            }
        }
    }
}
