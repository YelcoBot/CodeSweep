using System;
using System.Threading;
using System.Threading.Tasks;
using YelcoBot.CodeSweep.Domain.Results;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    /// <summary>Respuesta de un diálogo Sí / No / Cancelar.</summary>
    public enum UserChoice
    {
        Yes,
        No,
        Cancel
    }

    public interface IUserInteraction
    {
        Task<bool> ConfirmAsync(string title, string message);

        /// <summary>Diálogo Sí / No / Cancelar. Cerrar el diálogo equivale a Cancelar.</summary>
        Task<UserChoice> AskYesNoCancelAsync(string title, string message);

        Task ShowSummaryAsync(SweepSummary summary);
        Task RunWithProgressAsync(string title, Func<IProgress<SweepProgress>, CancellationToken, Task> action);
    }
}
