using System.IO;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Opciones de formato T-SQL que el producto (VS / SSMS) ya tiene en Tools → Options.
    /// Nada fijo en el código: se leen los manifiestos de Unified Settings registrados ([$RootKey$\SettingsManifests])
    /// y se toman las propiedades de un manifiesto de SQL (prefijo con "sql", como sqlFormatter.formatting.keywordCasing)
    /// cuyo nombre es una propiedad de ScriptDOM. Si SSMS agrega opciones en una actualización, CodeSweep las detecta sola.
    /// </summary>
    internal sealed class HostSqlFormatterSettings
    {
        private const string ManifestsCollection = "SettingsManifests";

        private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
        {
            CommentHandling = CommentHandling.Ignore,
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace
        };

        private static readonly HashSet<string> OptionNames = new HashSet<string>(
            SqlFormatterCatalog.Options.Select(o => o.Name), StringComparer.OrdinalIgnoreCase);

        /// <summary>Una sola detección por sesión: la usan las opciones y el paquete (visibilidad en Tools → Options).</summary>
        public static readonly HostSqlFormatterSettings Shared = new HostSqlFormatterSettings();

        private readonly AsyncLazy<IReadOnlyDictionary<string, HostSetting>> _settings;

        public HostSqlFormatterSettings()
        {
            _settings = new AsyncLazy<IReadOnlyDictionary<string, HostSetting>>(DiscoverAsync, ThreadHelper.JoinableTaskFactory);
        }

        /// <summary>Propiedad de ScriptDOM → opción del producto (moniker y valor por defecto).</summary>
        public Task<IReadOnlyDictionary<string, HostSetting>> GetAsync() => _settings.GetValueAsync();

        private static async Task<IReadOnlyDictionary<string, HostSetting>> DiscoverAsync()
        {
            Dictionary<string, HostSetting> result = new Dictionary<string, HostSetting>(StringComparer.OrdinalIgnoreCase);

            // Solo VS 2026 / SSMS 22+ tienen Unified Settings; VS 2022 no tiene opciones de formato SQL.
            if (!VsVersion.HasUnifiedSettings)
                return result;

            foreach (string manifestPath in await GetManifestPathsAsync())
            {
                foreach (KeyValuePair<string, HostSetting> setting in ReadSqlSettings(manifestPath))
                {
                    result[setting.Key] = setting.Value;
                }
            }

            return result;
        }

        private static async Task<List<string>> GetManifestPathsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            List<string> paths = new List<string>();
            try
            {
                SettingsStore store = new ShellSettingsManager(ServiceProvider.GlobalProvider).GetReadOnlySettingsStore(SettingsScope.Configuration);
                if (!store.CollectionExists(ManifestsCollection))
                    return paths;

                foreach (string manifest in store.GetSubCollectionNames(ManifestsCollection))
                {
                    string path = store.GetString(ManifestsCollection + "\\" + manifest, "ManifestPath", string.Empty);
                    if (path.Length > 0 && File.Exists(path))
                    {
                        paths.Add(path);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Colección mal formada: sin opciones del producto (se usan las de CodeSweep).
            }

            return paths;
        }

        private static IEnumerable<KeyValuePair<string, HostSetting>> ReadSqlSettings(string manifestPath)
        {
            JObject manifest;
            try
            {
                manifest = JObject.Parse(File.ReadAllText(manifestPath), LoadSettings);
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
            {
                yield break;
            }

            if (manifest["properties"] is not JObject properties)
                yield break;

            foreach (JProperty property in properties.Properties())
            {
                string moniker = property.Name;
                string prefix = moniker.Split('.')[0];
                string name = moniker.Substring(moniker.LastIndexOf('.') + 1);

                // Solo manifiestos de SQL (no los de CodeSweep ni opciones homónimas de otros editores, como indentationSize).
                if (prefix.IndexOf("sql", StringComparison.OrdinalIgnoreCase) < 0
                    || prefix.Equals("codeSweep", StringComparison.OrdinalIgnoreCase)
                    || !OptionNames.Contains(name))
                {
                    continue;
                }

                string optionName = OptionNames.First(n => n.Equals(name, StringComparison.OrdinalIgnoreCase));
                yield return new KeyValuePair<string, HostSetting>(optionName, new HostSetting(moniker, ToValue(property.Value["default"])));
            }
        }

        /// <summary>true / 4 / "Uppercase" → "true" / "4" / "Uppercase".</summary>
        internal static string? ToValue(JToken? token)
        {
            return token switch
            {
                JValue { Type: JTokenType.Boolean } value => (bool)value ? "true" : "false",
                JValue { Type: JTokenType.Integer } value => ((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture),
                JValue { Type: JTokenType.String } value => (string?)value,
                _ => null
            };
        }
    }

    /// <summary>Opción del producto: su clave en settings.json y su valor por defecto en el manifiesto.</summary>
    internal sealed class HostSetting
    {
        public HostSetting(string moniker, string? defaultValue)
        {
            Moniker = moniker;
            DefaultValue = defaultValue;
        }

        public string Moniker { get; }

        public string? DefaultValue { get; }
    }
}
