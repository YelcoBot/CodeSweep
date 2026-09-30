using System;
using System.Diagnostics;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>Versión de Visual Studio en la que corre la extensión.</summary>
    public static class VsVersion
    {
        private static readonly Lazy<int> Major = new Lazy<int>(() =>
        {
            try
            {
                return Process.GetCurrentProcess().MainModule?.FileVersionInfo.FileMajorPart ?? 0;
            }
            catch
            {
                return 0;
            }
        });

        /// <summary>
        /// VS 2026 (18.x) o superior: Tools → Options moderno (Unified Settings, settings.json).
        /// VS 2022 (17.x): Tools → Options clásico (página de propiedades).
        /// </summary>
        public static bool HasUnifiedSettings => Major.Value >= 18;
    }
}
