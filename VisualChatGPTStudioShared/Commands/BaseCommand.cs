using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    /// <summary>
    /// Base abstract class for commands
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    internal abstract class BaseCommand<TCommand> : Community.VisualStudio.Toolkit.BaseCommand<TCommand> where TCommand : class, new()
    {
        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).CancellationTokenSource;
            }
            set
            {
                ((VisuallChatGPTStudioPackage)this.Package).CancellationTokenSource = value;
            }
        }

        /// <summary>
        /// Gets the OptionsGeneral property of the VisualChatGPTStudioPackage.
        /// </summary>
        protected OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).OptionsGeneral;
            }
        }

        /// <summary>
        /// Gets the OptionsCommands property of the VisualChatGPTStudioPackage.
        /// </summary>
        protected OptionCommands OptionsCommands
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).OptionsCommands;
            }
        }

        /// <summary>
        /// Gets the DTE object.
        /// </summary>
        /// <returns>The DTE object.</returns>
        protected async System.Threading.Tasks.Task<DTE> GetDTEAsync()
        {
            return await VS.GetServiceAsync<DTE, DTE>();
        }

        /// <summary>
        /// Validates the API key stored in the OptionsGeneral class.
        /// </summary>
        /// <returns>
        /// Returns true if the API key is valid, false otherwise.
        /// </returns>
        protected async Task<bool> ValidateAPIKeyAsync()
        {
            if (string.IsNullOrWhiteSpace(OptionsGeneral.ApiKey))
            {
                await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, Constants.MESSAGE_SET_API_KEY, buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                Package.ShowOptionPage(typeof(OptionPageGridGeneral));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the code selected by the user.
        /// </summary>
        /// <param name="selectedCode">The selected code.</param>
        /// <returns>True if the code is valid, false otherwise.</returns>
        protected async Task<bool> ValidateCodeSelectedAsync(string selectedCode)
        {
            if (string.IsNullOrWhiteSpace(selectedCode))
            {
                await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, "Please select the code.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Formats the document.
        /// </summary>
        protected async System.Threading.Tasks.Task FormatDocumentAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                (await GetDTEAsync()).ExecuteCommand(Constants.EDIT_DOCUMENT_COMMAND);
            }
            catch (Exception) //Some documents do not support formatting
            {

            }
        }
    }
}
