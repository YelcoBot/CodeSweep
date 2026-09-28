using System.Threading.Tasks;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    public class VsSettingsStore : ISettingsStore
    {
        public async Task<SweepOptions> GetOptionsAsync()
        {
            GeneralOptions general = await GeneralOptions.GetLiveInstanceAsync();
            return new SweepOptions
            {
                EnableRemoveUnusedUsings = general.EnableRemoveUnusedUsings,
                EnableSortUsings = general.EnableSortUsings,
                EnableRemoveUnusedLocalVariables = general.EnableRemoveUnusedLocalVariables,
                EnableRemoveConsecutiveBlankLines = general.EnableRemoveConsecutiveBlankLines,
                EnableFormatDocument = general.EnableFormatDocument,
                CleanupOnSave = general.CleanupOnSave,
                IgnoreGeneratedCode = general.IgnoreGeneratedCode
            };
        }

        public async Task SaveOptionsAsync(SweepOptions options)
        {
            GeneralOptions general = await GeneralOptions.GetLiveInstanceAsync();
            general.EnableRemoveUnusedUsings = options.EnableRemoveUnusedUsings;
            general.EnableSortUsings = options.EnableSortUsings;
            general.EnableRemoveUnusedLocalVariables = options.EnableRemoveUnusedLocalVariables;
            general.EnableRemoveConsecutiveBlankLines = options.EnableRemoveConsecutiveBlankLines;
            general.EnableFormatDocument = options.EnableFormatDocument;
            general.CleanupOnSave = options.CleanupOnSave;
            general.IgnoreGeneratedCode = options.IgnoreGeneratedCode;

            await general.SaveAsync();
        }
    }
}
