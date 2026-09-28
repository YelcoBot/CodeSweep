using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using YelcoBot.CodeSweep.Application.UseCases;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Events
{
    /// <summary>
    /// Escucha la Running Document Table y limpia el documento ANTES de guardarlo,
    /// así lo que se escribe en disco ya está limpio y el documento no queda modificado.
    /// </summary>
    public class SaveEventListener : IVsRunningDocTableEvents3
    {
        private readonly CleanupOnSaveUseCase _cleanupOnSaveUseCase;
        private IVsRunningDocumentTable? _runningDocumentTable;
        private uint _cookie;

        public SaveEventListener(CleanupOnSaveUseCase cleanupOnSaveUseCase)
        {
            _cleanupOnSaveUseCase = cleanupOnSaveUseCase;
        }

        public async Task RegisterAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _runningDocumentTable = await VS.GetRequiredServiceAsync<SVsRunningDocumentTable, IVsRunningDocumentTable>();
            _runningDocumentTable.AdviseRunningDocTableEvents(this, out _cookie);
        }

        public int OnBeforeSave(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string? filePath = GetFilePath(docCookie);
            if (string.IsNullOrWhiteSpace(filePath))
                return VSConstants.S_OK;

            try
            {
                // El guardado espera a que termine el cleanup (igual que CodeMaid).
                ThreadHelper.JoinableTaskFactory.Run(() => _cleanupOnSaveUseCase.ExecuteAsync(filePath!));
            }
            catch (Exception ex)
            {
                // Un fallo del cleanup nunca debe impedir que el usuario guarde.
                ex.Log();
            }

            return VSConstants.S_OK;
        }

        private string? GetFilePath(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_runningDocumentTable == null)
                return null;

            int hr = _runningDocumentTable.GetDocumentInfo(docCookie, out _, out _, out _, out string moniker, out _, out _, out IntPtr docData);
            if (docData != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.Release(docData);
            }

            return ErrorHandler.Succeeded(hr) ? moniker : null;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
        public int OnAfterSave(uint docCookie) => VSConstants.S_OK;
        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;
        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;
        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => VSConstants.S_OK;
    }
}
