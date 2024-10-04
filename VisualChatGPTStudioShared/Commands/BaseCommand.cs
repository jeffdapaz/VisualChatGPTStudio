using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
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
        protected bool ValidateAPIKey()
        {
            if (OptionsGeneral.UseVisualStudioIdentity)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(OptionsGeneral.ApiKey))
            {
                System.Windows.Forms.MessageBox.Show(Constants.MESSAGE_SET_API_KEY, Constants.EXTENSION_NAME);

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
        protected bool ValidateCodeSelected(string selectedCode)
        {
            if (string.IsNullOrWhiteSpace(selectedCode))
            {
                System.Windows.Forms.MessageBox.Show("Please select the code.", Constants.EXTENSION_NAME);

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
