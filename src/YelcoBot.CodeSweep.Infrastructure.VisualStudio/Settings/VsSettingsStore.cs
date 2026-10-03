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
                return VsHost.IsSsms ? classic.ToSweepOptions().RestrictToSql() : classic.ToSweepOptions();
            }

            JObject settings = await _file.ReadAsync();
            SweepOptions defaults = new SweepOptions();

            bool Bool(string key, bool fallback) =>
                settings[key] is JValue { Type: JTokenType.Boolean } value ? (bool)value : fallback;

            int Int(string key, int fallback) =>
                settings[key] is JValue { Type: JTokenType.Integer } value ? (int)value : fallback;

            string Text(string key, string fallback) =>
                settings[key] is JValue { Type: JTokenType.String } value ? (string?)value ?? fallback : fallback;

            SweepOptions options = new SweepOptions
            {
                CleanupOnSave = Bool(SettingKeys.CleanupOnSave, defaults.CleanupOnSave),

                EnableRemoveUnusedLocalVariables = Bool(SettingKeys.RemoveUnusedLocalVariables, defaults.EnableRemoveUnusedLocalVariables),
                EnableRemoveUnusedUsings = Bool(SettingKeys.RemoveUnusedUsings, defaults.EnableRemoveUnusedUsings),
                EnableSortUsings = Bool(SettingKeys.SortUsings, defaults.EnableSortUsings),
                EnableFormatDocument = Bool(SettingKeys.FormatDocument, defaults.EnableFormatDocument),

                EnableRemoveConsecutiveBlankLines = Bool(SettingKeys.RemoveConsecutiveBlankLines, defaults.EnableRemoveConsecutiveBlankLines),
                MaxConsecutiveBlankLines = Int(SettingKeys.MaxConsecutiveBlankLines, defaults.MaxConsecutiveBlankLines),
                EnableRemoveLeadingBlankLines = Bool(SettingKeys.RemoveLeadingBlankLines, defaults.EnableRemoveLeadingBlankLines),
                EnableRemoveTrailingBlankLines = Bool(SettingKeys.RemoveTrailingBlankLines, defaults.EnableRemoveTrailingBlankLines),
                EnableTrimTrailingWhitespace = Bool(SettingKeys.TrimTrailingWhitespace, defaults.EnableTrimTrailingWhitespace),
                EnableSingleFinalNewline = Bool(SettingKeys.SingleFinalNewline, defaults.EnableSingleFinalNewline),

                EnableRemoveBlankLinesAfterOpenBlock = Bool(SettingKeys.RemoveBlankLinesAfterOpenBlock, defaults.EnableRemoveBlankLinesAfterOpenBlock),
                EnableRemoveBlankLinesBeforeCloseBlock = Bool(SettingKeys.RemoveBlankLinesBeforeCloseBlock, defaults.EnableRemoveBlankLinesBeforeCloseBlock),
                EnableRemoveBlankLinesAfterAttributes = Bool(SettingKeys.RemoveBlankLinesAfterAttributes, defaults.EnableRemoveBlankLinesAfterAttributes),
                EnableRemoveBlankLinesBetweenChainedCalls = Bool(SettingKeys.RemoveBlankLinesBetweenChainedCalls, defaults.EnableRemoveBlankLinesBetweenChainedCalls),
                EnableBlankLineBetweenMembers = Bool(SettingKeys.BlankLineBetweenMembers, defaults.EnableBlankLineBetweenMembers),
                BlankLineBetweenMemberKinds = Text(SettingKeys.BlankLineBetweenMemberKinds, defaults.BlankLineBetweenMemberKinds),
                EnableBlankLineAroundRegions = Bool(SettingKeys.BlankLineAroundRegions, defaults.EnableBlankLineAroundRegions),
                EnableBlankLineBeforeCase = Bool(SettingKeys.BlankLineBeforeCase, defaults.EnableBlankLineBeforeCase),
                EnableBlankLineBeforeSingleLineComments = Bool(SettingKeys.BlankLineBeforeSingleLineComments, defaults.EnableBlankLineBeforeSingleLineComments),
                EnableBlankLineAfterUsings = Bool(SettingKeys.BlankLineAfterUsings, defaults.EnableBlankLineAfterUsings),
                EnableRemoveAllRegions = Bool(SettingKeys.RemoveAllRegions, defaults.EnableRemoveAllRegions),
                EnableUpdateEndRegionText = Bool(SettingKeys.UpdateEndRegionText, defaults.EnableUpdateEndRegionText),
                EnableRemoveEmptyRegions = Bool(SettingKeys.RemoveEmptyRegions, defaults.EnableRemoveEmptyRegions),
                EnableReorganizeMembers = Bool(SettingKeys.ReorganizeMembers, defaults.EnableReorganizeMembers),
                MemberKindOrder = Text(SettingKeys.MemberKindOrder, defaults.MemberKindOrder),
                AccessOrder = Text(SettingKeys.AccessOrder, defaults.AccessOrder),
                EnableUniformAccessors = Bool(SettingKeys.UniformAccessors, defaults.EnableUniformAccessors),
                EnableWrapComments = Bool(SettingKeys.WrapComments, defaults.EnableWrapComments),
                CommentWrapColumn = Int(SettingKeys.CommentWrapColumn, defaults.CommentWrapColumn),
                EnableSpaceAfterCommentPrefix = Bool(SettingKeys.SpaceAfterCommentPrefix, defaults.EnableSpaceAfterCommentPrefix),
                EnableRemoveBlankLinesInsideTags = Bool(SettingKeys.RemoveBlankLinesInsideTags, defaults.EnableRemoveBlankLinesInsideTags),
                EnableRemoveEmptyComments = Bool(SettingKeys.RemoveEmptyComments, defaults.EnableRemoveEmptyComments),

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
                IncludeSql = Bool(SettingKeys.IncludeSql, defaults.IncludeSql),
                EnableFormatSql = Bool(SettingKeys.FormatSql, defaults.EnableFormatSql),
                AdditionalFileExtensions = Text(SettingKeys.AdditionalFileExtensions, defaults.AdditionalFileExtensions),

                IgnoreGeneratedCode = Bool(SettingKeys.IgnoreGeneratedCode, defaults.IgnoreGeneratedCode),
                ExcludeT4GeneratedCode = Bool(SettingKeys.ExcludeT4GeneratedCode, defaults.ExcludeT4GeneratedCode),
                ExcludePatterns = Text(SettingKeys.ExcludePatterns, defaults.ExcludePatterns)
            };

            options.SqlFormatter = await ResolveSqlFormatterAsync(settings);

            return VsHost.IsSsms ? options.RestrictToSql() : options;
        }

        /// <summary>
        /// Opciones del formateador T-SQL: las de CodeSweep (o su default) y, encima, las del producto para las que tiene
        /// (valor guardado en settings.json o el default de su manifiesto). El .editorconfig gana después, en el motor.
        /// </summary>
        private static async Task<Dictionary<string, string>> ResolveSqlFormatterAsync(JObject settings)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (SqlFormatterOption option in SqlFormatterCatalog.Options)
            {
                values[option.Name] = HostSqlFormatterSettings.ToValue(settings[SettingKeys.SqlFormatter(option)]) ?? option.DefaultValue;
            }

            foreach (KeyValuePair<string, HostSetting> host in await HostSqlFormatterSettings.Shared.GetAsync())
            {
                string? value = HostSqlFormatterSettings.ToValue(settings[host.Value.Moniker]) ?? host.Value.DefaultValue;
                if (value != null)
                {
                    values[host.Key] = value;
                }
            }

            return values;
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
