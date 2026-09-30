using System.Globalization;
using System.Resources;

namespace YelcoBot.CodeSweep.Application.Localization
{
    /// <summary>
    /// Textos de la extensión en inglés (Strings.resx) y español (Strings.es.resx).
    /// El idioma sale de VS: el paquete asigna <see cref="Culture"/> al cargar.
    /// </summary>
    public static class Strings
    {
        private static readonly ResourceManager Manager =
            new ResourceManager("YelcoBot.CodeSweep.Application.Localization.Strings", typeof(Strings).Assembly);

        /// <summary>Idioma de la interfaz de VS. Null = CurrentUICulture del hilo.</summary>
        public static CultureInfo? Culture { get; set; }

        public static string Get(string key) => Manager.GetString(key, Culture) ?? key;

        public static string Format(string key, params object[] args) =>
            string.Format(Culture ?? CultureInfo.CurrentUICulture, Get(key), args);

        // Menús
        public static string CommandCleanupActiveDocument => Get(nameof(CommandCleanupActiveDocument));
        public static string CommandCleanupOpenCode => Get(nameof(CommandCleanupOpenCode));
        public static string CommandCleanupAllCode => Get(nameof(CommandCleanupAllCode));
        public static string CommandCleanupOnSave => Get(nameof(CommandCleanupOnSave));
        public static string CommandCleanupSelectedCode => Get(nameof(CommandCleanupSelectedCode));

        // Progreso
        public static string ProgressActiveDocument => Get(nameof(ProgressActiveDocument));
        public static string ProgressOpenDocuments => Get(nameof(ProgressOpenDocuments));
        public static string ProgressSelectedItems => Get(nameof(ProgressSelectedItems));
        public static string ProgressSolution => Get(nameof(ProgressSolution));

        // Confirmaciones
        public static string CommonContinue => Get(nameof(CommonContinue));
        public static string SolutionConfirmTitle => Get(nameof(SolutionConfirmTitle));
        public static string SolutionConfirmMessage => Get(nameof(SolutionConfirmMessage));
        public static string SelectedItemsTitle => Get(nameof(SelectedItemsTitle));
        public static string SelectedItemsIncludesFilesAndContainers => Get(nameof(SelectedItemsIncludesFilesAndContainers));
        public static string SelectedItemsIncludesContainers => Get(nameof(SelectedItemsIncludesContainers));
        public static string SelectedItemsOptionYes => Get(nameof(SelectedItemsOptionYes));
        public static string SelectedItemsOptionCancel => Get(nameof(SelectedItemsOptionCancel));

        // Tipos de contenedor
        public static string KindSolution => Get(nameof(KindSolution));
        public static string KindSolutionFolder => Get(nameof(KindSolutionFolder));
        public static string KindProject => Get(nameof(KindProject));
        public static string KindFolder => Get(nameof(KindFolder));

        // Resumen y errores
        public static string SummaryTitle => Get(nameof(SummaryTitle));
        public static string SummaryCancelled => Get(nameof(SummaryCancelled));
        public static string FailureWorkspaceChanged => Get(nameof(FailureWorkspaceChanged));
        public static string DesignerNoSolutionExplorer => Get(nameof(DesignerNoSolutionExplorer));
        public static string DesignerNotInSolution => Get(nameof(DesignerNotInSolution));
        public static string DesignerCommandNotAvailable => Get(nameof(DesignerCommandNotAvailable));

        // Con formato ({0}, {1}…): usar Format(nameof(...), args)
        public const string SelectedItemsOptionNo = nameof(SelectedItemsOptionNo);
        public const string SelectedItemsContainerLine = nameof(SelectedItemsContainerLine);
        public const string SelectedItemsMore = nameof(SelectedItemsMore);
        public const string SelectedItemsContainersTotal = nameof(SelectedItemsContainersTotal);
        public const string SummaryProcessed = nameof(SummaryProcessed);
        public const string SummaryChanged = nameof(SummaryChanged);
        public const string SummaryDuration = nameof(SummaryDuration);
        public const string SummaryFailures = nameof(SummaryFailures);
        public const string SummaryStatusBar = nameof(SummaryStatusBar);
        public const string DesignerFailed = nameof(DesignerFailed);
        public const string EditorCannotFormatInBackground = nameof(EditorCannotFormatInBackground);
        public const string LogCleanupOnSave = nameof(LogCleanupOnSave);
    }
}
