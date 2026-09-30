using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings;

namespace YelcoBot.CodeSweep.Options
{
    /// <summary>
    /// Página clásica de Tools → Options para VS 2022.
    /// En VS 2026 la reemplaza la página moderna: CodeSweep.registration.json declara
    /// "legacyOptionPageId" con este mismo GUID.
    /// </summary>
    [ComVisible(true)]
    [Guid(PageGuid)]
    public class ClassicOptionsPage : BaseOptionPage<ClassicOptions>
    {
        public const string PageGuid = "c7d4e2a1-5b3f-4e8a-9c6d-2f1a0b3e4d5c";
    }
}
