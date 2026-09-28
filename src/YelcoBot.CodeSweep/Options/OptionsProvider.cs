using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;
using YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings;

namespace YelcoBot.CodeSweep.Options
{
    public class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptionsPage : BaseOptionPage<GeneralOptions> { }
    }
}
