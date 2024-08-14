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
    /// Exports the IVsTextViewCreationListener to handle the creation of text views in Visual Studio.
    /// </summary>
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class TextViewCreationListener : IVsTextViewCreationListener
    {
        /// <summary>
        /// Gets or sets the IVsEditorAdaptersFactoryService instance used to create and manage editor adapters.
        /// </summary>
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        /// <summary>
        /// Handles the creation of a Visual Studio text view, initializing necessary components such as the prediction manager and command filter.
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

            InlinePredictionManager predictionManager = new(view);

            CommandFilter commandFilter = new(view, predictionManager, package.OptionsGeneral);
            commandFilter.AttachToView(textViewAdapter);
        }

        /// <summary>
        /// Asynchronously retrieves the VisuallChatGPTStudioPackage instance.
        /// </summary>
        /// <returns>
        /// The task result contains the VisuallChatGPTStudioPackage instance if found; otherwise, null.
        /// </returns>
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
    }
}