using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using YelcoBot.CodeSweep.Domain.Options;

using OleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Editor
{
    /// <summary>
    /// Estrategia 2: documento cerrado → cargarlo con IVsInvisibleEditorManager (sin ventana),
    /// crear una vista oculta, formatear, guardar y liberar. Es la vía para todos los archivos del editor,
    /// excepto los Web Forms que deben pasar por el editor real (ver EditorFormatRules).
    /// Si para una extensión no funciona, se recuerda para no reintentar (y se informa en el resumen; no se abren ventanas).
    /// </summary>
    public class InvisibleEditorFormatStrategy : IEditorFormatStrategy
    {
        private readonly ConcurrentDictionary<string, bool> _unsupportedExtensions = new(StringComparer.OrdinalIgnoreCase);

        public int Order => 2;

        public async Task<EditorFormatOutcome> TryFormatAsync(string filePath, SweepOptions options, CancellationToken cancellationToken)
        {
            if (EditorFormatRules.UsesRealEditor(filePath, options))
                return EditorFormatOutcome.NotHandled;

            string extension = Path.GetExtension(filePath);
            if (_unsupportedExtensions.ContainsKey(extension))
                return EditorFormatOutcome.NotHandled;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            IVsInvisibleEditorManager manager = await VS.GetRequiredServiceAsync<SVsInvisibleEditorManager, IVsInvisibleEditorManager>();
            IVsEditorAdaptersFactoryService adapters = await VS.GetMefServiceAsync<IVsEditorAdaptersFactoryService>();
            ITextEditorFactoryService editorFactory = await VS.GetMefServiceAsync<ITextEditorFactoryService>();

            IVsInvisibleEditor? invisibleEditor = null;
            IVsTextView? view = null;
            try
            {
                int hr = manager.RegisterInvisibleEditor(filePath, null, (uint)_EDITORREGFLAGS.RIEF_ENABLECACHING, null, out invisibleEditor);
                if (ErrorHandler.Failed(hr) || invisibleEditor == null)
                    return MarkUnsupported(extension);

                IVsTextLines? textLines = GetTextLines(invisibleEditor);
                if (textLines == null)
                    return MarkUnsupported(extension);

                view = CreateHiddenView(textLines, adapters, editorFactory);
                if (view == null)
                    return MarkUnsupported(extension);

                EditorFormatOutcome outcome = await FormatDocumentCommand.ExecuteAsync(view, adapters, filePath, options, FormatDocumentCommand.InvisibleEditorTimeout, cancellationToken);
                if (outcome == EditorFormatOutcome.NotHandled)
                    return MarkUnsupported(extension);

                if (outcome == EditorFormatOutcome.Changed && textLines is IVsPersistDocData persist)
                {
                    ErrorHandler.ThrowOnFailure(persist.SaveDocData(VSSAVEFLAGS.VSSAVE_SilentSave, out _, out _));
                }

                return outcome;
            }
            finally
            {
                view?.CloseView();
                if (invisibleEditor != null)
                {
                    Marshal.ReleaseComObject(invisibleEditor);
                }
            }
        }

        private EditorFormatOutcome MarkUnsupported(string extension)
        {
            _unsupportedExtensions[extension] = true;
            return EditorFormatOutcome.NotHandled;
        }

        private static IVsTextLines? GetTextLines(IVsInvisibleEditor invisibleEditor)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid textLinesGuid = typeof(IVsTextLines).GUID;
            if (ErrorHandler.Failed(invisibleEditor.GetDocData(fEnsureWritable: 1, ref textLinesGuid, out IntPtr docDataPtr)) || docDataPtr == IntPtr.Zero)
                return null;

            try
            {
                return Marshal.GetObjectForIUnknown(docDataPtr) as IVsTextLines;
            }
            finally
            {
                Marshal.Release(docDataPtr);
            }
        }

        private static IVsTextView? CreateHiddenView(
            IVsTextLines textLines,
            IVsEditorAdaptersFactoryService adapters,
            ITextEditorFactoryService editorFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleServiceProvider oleServiceProvider = (OleServiceProvider)Package.GetGlobalService(typeof(OleServiceProvider));

            // Roles de documento editable: así los servicios de lenguaje enganchan sus comandos (formato incluido).
            ITextViewRoleSet roles = editorFactory.CreateTextViewRoleSet(
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Interactive,
                PredefinedTextViewRoles.Structured,
                PredefinedTextViewRoles.Analyzable);

            IVsTextView view = adapters.CreateVsTextViewAdapter(oleServiceProvider, roles);

            uint flags = (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT
                       | (uint)TextViewInitFlags2.VIF_SUPPRESSBORDER
                       | (uint)TextViewInitFlags2.VIF_SUPPRESS_STATUS_BAR_UPDATE;

            if (ErrorHandler.Failed(view.Initialize(textLines, IntPtr.Zero, flags, null)))
            {
                view.CloseView();
                return null;
            }

            return view;
        }
    }
}
