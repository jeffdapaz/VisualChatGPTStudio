using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualChatGPTStudioShared.Utils;
using Clipboard = System.Windows.Clipboard;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace JeffPires.VisualChatGPTStudio.ToolWindows
{
    /// <summary>
    /// Interaction logic for TerminalWindowControl.
    /// </summary>
    public partial class TerminalWindowControl : UserControl
    {
        #region Properties

        private OptionPageGridGeneral options;
        private Package package;
        private bool firstIteration;
        private bool responseStarted;
        private CancellationTokenSource cancellationTokenSource;
        private bool removeCodeTagsFromOpenAIResponses;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowControl"/> class.
        /// </summary>
        public TerminalWindowControl()
        {
            this.InitializeComponent();
        }

        #endregion Constructors

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the btnRequestSend control.
        /// </summary>
        public async void SendRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (!options.UseVisualStudioIdentity && string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    MessageBox.Show(Constants.MESSAGE_SET_API_KEY, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                    package.ShowOptionPage(typeof(OptionPageGridGeneral));

                    return;
                }

                firstIteration = true;
                responseStarted = false;

                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                EnableDisableButtons(false, true);

                txtResponse.Text = string.Empty;

                cancellationTokenSource = new CancellationTokenSource();

                removeCodeTagsFromOpenAIResponses = false;

                if (options.SingleResponse)
                {
                    string result = await ChatGPT.GetResponseAsync(options, options.ToolWindowSystemMessage, txtRequest.Text, options.StopSequences.Split(','), cancellationTokenSource.Token);

                    ResultHandler(result);
                }
                else
                {
                    await ChatGPT.GetResponseAsync(options, options.ToolWindowSystemMessage, txtRequest.Text, options.StopSequences.Split(','), ResultHandler, cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                EnableDisableButtons(true, false);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                EnableDisableButtons(true, false);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the click event of the Generate Git Comment button. It generates a Git comment based on the current changes using ChatGPT.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void btnGenerateGitComment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableDisableButtons(false, true);

                string changes = GitChanges.GetCurrentChanges();

                if (string.IsNullOrWhiteSpace(changes))
                {
                    MessageBox.Show("No changes found.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);

                    return;
                }

                txtRequest.Text = string.Concat(options.GenerateGitCommentCommand, Environment.NewLine, Environment.NewLine, changes);
                txtResponse.Text = string.Empty;

                cancellationTokenSource = new CancellationTokenSource();

                removeCodeTagsFromOpenAIResponses = true;

                string comment = await ChatGPT.GetResponseAsync(options, options.GenerateGitCommentCommand, changes, options.StopSequences.Split(','), cancellationTokenSource.Token);

                ResultHandler(comment);
            }
            catch (OperationCanceledException)
            {
                //Do nothing
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show("Git repository not found.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                EnableDisableButtons(true, false);
            }
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public async void CancelRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            btnCancel.IsEnabled = false;

            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Handles the Click event of the btnRequestPast control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        [STAThread]
        private void btnRequestPast_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                txtRequest.AppendText(Clipboard.GetText());
            }
        }

        /// <summary>
        /// Clears the request text document.
        /// </summary>
        /// <param name="sender">The button click sender.</param>
        /// <param name="e">Routed event args.</param>
        private void btnRequestClear_Click(object sender, RoutedEventArgs e)
        {
            txtRequest.Text = string.Empty;
        }

        /// <summary>
        /// Copy the content of the Response TextBox to the Clipboard.
        /// </summary>
        /// <param name="sender">The button that invokes the event.</param>
        /// <param name="e">Event arguments.</param>
        private void btnResponseCopy_Click(object sender, RoutedEventArgs e)
        {
            TerminalWindowHelper.Copy((Image)sender, txtResponse.Text);
        }

        /// <summary>
        /// Event handler for the btnSwitchWordWrap button click event. Toggles the visibility of the horizontal scroll bar in the txtRequest TextBox.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnSwitchWordWrap_Click(object sender, RoutedEventArgs e)
        {
            if (txtResponse.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
            {
                txtResponse.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                txtResponse.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
        }

        /// <summary>
        /// This method changes the syntax highlighting of the textbox based on the language detected in the text.
        /// </summary>
        private void txtRequest_TextChanged(object sender, EventArgs e)
        {
            txtRequest.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition(TextFormat.DetectCodeLanguage(txtRequest.Text));
        }

        #endregion Event Handlers

        #region Methods

        /// <summary>
        /// Starts the control with the given options and package.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="package">The package.</param>
        public void StartControl(OptionPageGridGeneral options, Package package)
        {
            this.options = options;
            this.package = package;
        }

        /// <summary>
        /// Handles the result of an operation and appends it to the end of the txtResponse control.
        /// </summary>
        /// <param name="result">The result of the operation.</param>
        private async void ResultHandler(string result)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (firstIteration)
            {
                EnableDisableButtons(true, true);

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 2, 2);

                firstIteration = false;
                responseStarted = false;
            }

            if (!options.SingleResponse && !responseStarted && (result.Equals("\n") || result.Equals("\r") || result.Equals(Environment.NewLine)))
            {
                //Do nothing when API send only break lines on response begin
                return;
            }

            responseStarted = true;

            if (removeCodeTagsFromOpenAIResponses)
            {
                result = TextFormat.RemoveCodeTagsFromOpenAIResponses(result);
            }
            else if (options.SingleResponse)
            {
                result = TextFormat.RemoveBlankLinesFromResult(result);
            }

            txtResponse.AppendText(result);

            txtResponse.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition(TextFormat.DetectCodeLanguage(txtResponse.Text));

            txtResponse.ScrollToEnd();
        }

        /// <summary>
        /// Requests to the chatGPT window with the given command and selected text.
        /// </summary>
        /// <param name="command">The command to be sent.</param>
        /// <param name="selectedText">The selected text to be sent.</param>
        /// <param name="removeCodeTagsFromOpenAIResponses">Indicates if the code tags from OpenAI responses need to be removed from the result.</param>
        public async System.Threading.Tasks.Task RequestToWindowAsync(string command, string selectedText, bool removeCodeTagsFromOpenAIResponses)
        {
            try
            {
                firstIteration = true;

                this.removeCodeTagsFromOpenAIResponses = removeCodeTagsFromOpenAIResponses;

                await VS.StatusBar.ShowProgressAsync("Requesting chatGPT", 1, 2);

                EnableDisableButtons(false, true);

                txtRequest.Text = command + Environment.NewLine + Environment.NewLine + selectedText;

                txtResponse.Text = string.Empty;

                cancellationTokenSource = new CancellationTokenSource();

                if (options.SingleResponse || removeCodeTagsFromOpenAIResponses)
                {
                    string result = await ChatGPT.GetResponseAsync(options, command, selectedText, options.StopSequences.Split(','), cancellationTokenSource.Token);

                    ResultHandler(result);
                }
                else
                {
                    await ChatGPT.GetResponseAsync(options, command, selectedText, options.StopSequences.Split(','), ResultHandler, cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                EnableDisableButtons(true, false);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                EnableDisableButtons(true, false);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Enables or disables the send button and cancel button based on the provided parameters.
        /// </summary>
        /// <param name="enableSendButton">A boolean value indicating whether to enable or disable the send button.</param>
        /// <param name="enableCancelButton">A boolean value indicating whether to enable or disable the cancel button.</param>
        private async void EnableDisableButtons(bool enableSendButton, bool enableCancelButton)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            grdProgress.Visibility = enableSendButton ? Visibility.Collapsed : Visibility.Visible;

            btnRequestSend.IsEnabled = enableSendButton;
            btnCancel.IsEnabled = enableCancelButton;
        }

        #endregion Methods        
    }
}