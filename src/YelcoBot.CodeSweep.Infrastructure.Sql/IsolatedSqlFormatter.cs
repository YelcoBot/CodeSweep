using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Localization;
using YelcoBot.CodeSweep.SqlEngine;

namespace YelcoBot.CodeSweep.Infrastructure.Sql
{
    /// <summary>
    /// Formatea T-SQL con el motor de CodeSweep (YelcoBot.CodeSweep.SqlEngine) en un AppDomain aparte,
    /// cargado desde la subcarpeta SqlEngine\ de la extensión:
    /// - Siempre se usa nuestro ScriptDOM (la última versión), también en SSMS, que trae otro con la misma identidad.
    /// - EditorConfig y sus dependencias no chocan con las del producto (ese dominio no usa SSMS.exe.config ni devenv.exe.config).
    /// El dominio se crea una vez y se reutiliza.
    /// </summary>
    public sealed class IsolatedSqlFormatter : ISqlFormatter, IDisposable
    {
        private const string EngineFolder = "SqlEngine";
        private const string EngineTypeName = "YelcoBot.CodeSweep.SqlEngine.SqlEngine";

        private readonly object _gate = new object();
        private AppDomain? _domain;
        private ISqlEngine? _engine;

        public bool TryFormat(string text, string filePath, IReadOnlyDictionary<string, string> settings, out string formatted, out string? reason)
        {
            formatted = text;

            ISqlEngine engine = GetEngine();
            SqlFormatResult result = engine.Format(text, filePath, settings.ToDictionary(s => s.Key, s => s.Value));

            if (result.Success)
            {
                formatted = result.Text;
                reason = null;
                return true;
            }

            reason = result.Discarded
                ? Strings.Get(Strings.SqlResultDiscarded)
                : Strings.Format(Strings.SqlParseError, result.ErrorLine, result.ErrorColumn, result.ErrorMessage ?? string.Empty);
            return false;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_domain != null)
                {
                    AppDomain.Unload(_domain);
                    _domain = null;
                    _engine = null;
                }
            }
        }

        private ISqlEngine GetEngine()
        {
            lock (_gate)
            {
                if (_engine != null)
                    return _engine;

                string extensionFolder = Path.GetDirectoryName(typeof(IsolatedSqlFormatter).Assembly.Location)!;
                string engineFolder = Path.Combine(extensionFolder, EngineFolder);

                AppDomainSetup setup = new AppDomainSetup
                {
                    ApplicationBase = engineFolder,
                    ApplicationName = "CodeSweep.SqlEngine",
                    DisallowBindingRedirects = false
                };

                _domain = AppDomain.CreateDomain("CodeSweep.SqlEngine", null, setup);

                // Por nombre: el tipo del motor (que usa ScriptDOM) nunca se carga en este dominio.
                _engine = (ISqlEngine)_domain.CreateInstanceAndUnwrap(typeof(ISqlEngine).Assembly.FullName, EngineTypeName);

                return _engine;
            }
        }
    }
}
