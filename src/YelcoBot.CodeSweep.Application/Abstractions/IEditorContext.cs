using System.Threading.Tasks;

namespace YelcoBot.CodeSweep.Application.Abstractions
{
    public interface IEditorContext
    {
        Task<string?> GetActiveDocumentPathAsync();
    }
}
