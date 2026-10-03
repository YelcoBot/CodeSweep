using System.Diagnostics;
using System.IO;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>Producto en el que corre la extensión: Visual Studio o SQL Server Management Studio (SSMS 22.7+).</summary>
    public static class VsHost
    {
        /// <summary>
        /// UIContext que el paquete activa al cargar en SSMS. CodeSweep.registration.json lo usa en "visibleWhen"
        /// para ocultar en SSMS las opciones de C#, VB, markup y demás lenguajes que SSMS no usa.
        /// </summary>
        public const string SsmsUIContextString = "48de90ae-eb39-443f-8cb2-6cc39c85ad20";
        public static readonly Guid SsmsUIContext = new Guid(SsmsUIContextString);

        private static readonly Lazy<bool> IsSsmsProcess = new Lazy<bool>(() =>
        {
            try
            {
                string? path = Process.GetCurrentProcess().MainModule?.FileName;
                return string.Equals(Path.GetFileNameWithoutExtension(path), "Ssms", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        });

        /// <summary>SSMS: solo se limpia SQL (las reglas universales también aplican).</summary>
        public static bool IsSsms => IsSsmsProcess.Value;
    }
}
