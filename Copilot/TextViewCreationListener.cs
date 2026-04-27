using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Copilot
{
    /// <summary>
    /// Exports an <see cref="IVsTextViewCreationListener"/> that wires up the
    /// inline prediction manager for every editable code view.
    /// </summary>
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class TextViewCreationListener : IVsTextViewCreationListener
    {
        #region Properties

        /// <summary>
        /// Adapter service used to obtain the WPF text view from the legacy
        /// <see cref="IVsTextView"/>.
        /// </summary>
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handles the creation of a Visual Studio text view, attaching the
        /// inline prediction manager and command filter to it.
        /// </summary>
        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdapterService.GetWpfTextView(textViewAdapter);

            if (view == null)
            {
                return;
            }

            VisuallChatGPTStudioPackage package = GetPackageAsync().Result;

            if (package == null)
            {
                return;
            }

            view.Properties.GetOrCreateSingletonProperty(typeof(Microsoft.VisualStudio.TextManager.Interop.IVsTextView), () => textViewAdapter);

            InlinePredictionManager manager = new(package.OptionsGeneral, view);
            view.Properties.GetOrCreateSingletonProperty(typeof(InlinePredictionManager), () => manager);

            _ = new CommandFilter(view, textViewAdapter);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Asynchronously retrieves the <see cref="VisuallChatGPTStudioPackage"/>.
        /// </summary>
        private async Task<VisuallChatGPTStudioPackage> GetPackageAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsShell shell = (IVsShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsShell));

            if (shell != null)
            {
                shell.LoadPackage(ref PackageGuids.VisuallChatGPTStudio, out IVsPackage package);

                return package as VisuallChatGPTStudioPackage;
            }

            return null;
        }

        #endregion
    }
}
