using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Opciones de CodeSweep según la versión de VS:
    /// - VS 2026+: settings.json (Unified Settings, Tools → Options moderno). Clave ausente = default.
    /// - VS 2022: página clásica de Tools → Options (ClassicOptions).
    /// </summary>
    public class VsSettingsStore : ISettingsStore
    {
        private readonly UnifiedSettingsFile _file = new UnifiedSettingsFile();

        public async Task<SweepOptions> GetOptionsAsync()
        {
            if (!VsVersion.HasUnifiedSettings)
            {
                ClassicOptions classic = await ClassicOptions.GetLiveInstanceAsync();
                return classic.ToSweepOptions();
            }

            JObject settings = await _file.ReadAsync();
            SweepOptions defaults = new SweepOptions();

            bool Bool(string key, bool fallback) =>
                settings[key] is JValue { Type: JTokenType.Boolean } value ? (bool)value : fallback;

            string Text(string key, string fallback) =>
                settings[key] is JValue { Type: JTokenType.String } value ? (string?)value ?? fallback : fallback;

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
            // Desde código solo se cambia "cleanup on save" (botón del menú); el resto se edita en Tools → Options.
            if (!VsVersion.HasUnifiedSettings)
            {
                ClassicOptions classic = await ClassicOptions.GetLiveInstanceAsync();
                classic.CleanupOnSave = options.CleanupOnSave;
                await classic.SaveAsync();
                return;
            }

            await _file.WriteValueAsync(SettingKeys.CleanupOnSave, new JValue(options.CleanupOnSave));
        }
    }
}
