using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace JeffPires.VisualChatGPTStudio.ToolWindows
{
    /// <summary>
    /// Interaction logic for TerminalWindowTurboControl.
    /// </summary>
    public partial class TerminalWindowTurboControl : UserControl
    {
        #region Constants

        const string EXTENSION_NAME = "Visual chatGPT Studio";

        #endregion Constants

        #region Properties

        private OptionPageGridGeneral options;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowTurboControl"/> class.
        /// </summary>
        public TerminalWindowTurboControl()
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
                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    MessageBox.Show("Please write a request.", EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await VS.StatusBar.ShowProgressAsync("Requesting chatGPT", 1, 2);


            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                MessageBox.Show(ex.Message, EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Clear the conversation?", EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (result == MessageBoxResult.No)
            {
                return;
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
        /// Starts the control by checking if the OpenAI API key is set. If not, it shows a warning message and the option page.
        /// </summary>
        /// <param name="options">The options page.</param>
        /// <param name="package">The package.</param>
        public void StartControl(OptionPageGridGeneral options, Package package)
        {
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                this.options = options;

                return;
            }

            string message = "Please, set the OpenAI API key and restart Visual Studio.";

            MessageBox.Show(message, EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

            package.ShowOptionPage(typeof(OptionPageGridGeneral));
        }

        #endregion Methods    
    }
}