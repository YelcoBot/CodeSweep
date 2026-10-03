using System.IO;
using System.Text;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// settings.json de la instancia de VS (%LOCALAPPDATA%\Microsoft\VisualStudio\18.0_xxx\settings.json):
    /// ahí guarda Tools → Options moderno (Unified Settings) lo que el usuario cambia, con la clave del registration.json
    /// (ej. "codeSweep.fileTypes.includeWebForms": false). Si una clave no está, vale su default.
    /// </summary>
    internal sealed class UnifiedSettingsFile
    {
        private const string Header = "/* Visual Studio Settings File */";

        private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
        {
            CommentHandling = CommentHandling.Ignore,
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace
        };

        private string? _path;

        public async Task<JObject> ReadAsync()
        {
            string path = await GetPathAsync();

            try
            {
                if (!File.Exists(path))
                    return new JObject();

                return JObject.Parse(File.ReadAllText(path), LoadSettings);
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
            {
                // Archivo en uso o inválido: se usan los defaults, nunca se rompe un cleanup.
                return new JObject();
            }
        }

        public async Task WriteValueAsync(string moniker, JToken value)
        {
            string path = await GetPathAsync();

            JObject settings = await ReadAsync();
            settings[moniker] = value;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, Header + Environment.NewLine + settings.ToString(Formatting.Indented), new UTF8Encoding(false));
        }

        private async Task<string> GetPathAsync()
        {
            if (_path != null)
                return _path;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Carpeta local de ESTA instancia de VS (incluye el sufijo "Exp" en la instancia experimental).
            ShellSettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            string folder = settingsManager.GetApplicationDataFolder(ApplicationDataFolder.LocalSettings);

            _path = Path.Combine(folder, "settings.json");
            return _path;
        }
    }
}
