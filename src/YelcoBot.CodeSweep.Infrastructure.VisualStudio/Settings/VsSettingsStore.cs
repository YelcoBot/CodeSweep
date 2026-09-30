using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Lee/escribe las opciones en el SettingsManager de VS.
    /// La página moderna de Tools → Options (Unified Settings, CodeSweep.registration.json) guarda en estas mismas claves
    /// gracias a "migration": { "pass": { "store": "SettingsManager" } }.
    /// </summary>
    public class VsSettingsStore : ISettingsStore
    {
        public async Task<SweepOptions> GetOptionsAsync()
        {
            ISettingsManager settings = await GetSettingsManagerAsync();
            SweepOptions defaults = new SweepOptions();

            bool Bool(string key, bool fallback) => settings.GetValueOrDefault(key, fallback);
            string Text(string key, string fallback) => settings.GetValueOrDefault(key, fallback) ?? fallback;

            return new SweepOptions
            {
                CleanupOnSave = Bool(SettingKeys.CleanupOnSave, defaults.CleanupOnSave),

                EnableRemoveUnusedLocalVariables = Bool(SettingKeys.RemoveUnusedLocalVariables, defaults.EnableRemoveUnusedLocalVariables),
                EnableRemoveUnusedUsings = Bool(SettingKeys.RemoveUnusedUsings, defaults.EnableRemoveUnusedUsings),
                EnableSortUsings = Bool(SettingKeys.SortUsings, defaults.EnableSortUsings),
                EnableRemoveConsecutiveBlankLines = Bool(SettingKeys.RemoveConsecutiveBlankLines, defaults.EnableRemoveConsecutiveBlankLines),
                EnableFormatDocument = Bool(SettingKeys.FormatDocument, defaults.EnableFormatDocument),

                IncludeCSharp = Bool(SettingKeys.IncludeCSharp, defaults.IncludeCSharp),
                IncludeVisualBasic = Bool(SettingKeys.IncludeVisualBasic, defaults.IncludeVisualBasic),
                IncludeWebForms = Bool(SettingKeys.IncludeWebForms, defaults.IncludeWebForms),
                RegenerateWebFormsDesigner = Bool(SettingKeys.RegenerateWebFormsDesigner, defaults.RegenerateWebFormsDesigner),
                IncludeRazor = Bool(SettingKeys.IncludeRazor, defaults.IncludeRazor),
                IncludeHtml = Bool(SettingKeys.IncludeHtml, defaults.IncludeHtml),
                IncludeXmlConfig = Bool(SettingKeys.IncludeXmlConfig, defaults.IncludeXmlConfig),
                IncludeXaml = Bool(SettingKeys.IncludeXaml, defaults.IncludeXaml),
                IncludeStyles = Bool(SettingKeys.IncludeStyles, defaults.IncludeStyles),
                IncludeScripts = Bool(SettingKeys.IncludeScripts, defaults.IncludeScripts),
                IncludeJson = Bool(SettingKeys.IncludeJson, defaults.IncludeJson),
                AdditionalFileExtensions = Text(SettingKeys.AdditionalFileExtensions, defaults.AdditionalFileExtensions),

                IgnoreGeneratedCode = Bool(SettingKeys.IgnoreGeneratedCode, defaults.IgnoreGeneratedCode),
                ExcludeT4GeneratedCode = Bool(SettingKeys.ExcludeT4GeneratedCode, defaults.ExcludeT4GeneratedCode),
                ExcludePatterns = Text(SettingKeys.ExcludePatterns, defaults.ExcludePatterns)
            };
        }

        public async Task SaveOptionsAsync(SweepOptions options)
        {
            // Hoy solo se modifica desde código el "cleanup on save" (botón del menú); el resto se edita en Tools → Options.
            ISettingsManager settings = await GetSettingsManagerAsync();
            await settings.SetValueAsync(SettingKeys.CleanupOnSave, options.CleanupOnSave, isMachineLocal: false);
        }

        private static async Task<ISettingsManager> GetSettingsManagerAsync()
        {
            return (ISettingsManager)await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(Microsoft.Internal.VisualStudio.Shell.Interop.SVsSettingsPersistenceManager));
        }
    }
}
