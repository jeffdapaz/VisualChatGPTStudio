using JeffPires.VisualChatGPTStudio.Options;
using OpenAI_API.Completions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using TextRange = System.Windows.Documents.TextRange;
using UserControl = System.Windows.Controls.UserControl;

namespace JeffPires.VisualChatGPTStudio.ToolWindows
{
    /// <summary>
    /// Interaction logic for TerminalWindowControl.
    /// </summary>
    public partial class TerminalWindowControl : UserControl
    {
        #region Constants

        const string EXTENSION_NAME = "Visual chatGPT Studio";

        #endregion Constants

        #region Properties

        private OptionPageGridGeneral options;
        private bool firstInteration;

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
                firstInteration = true;

                TextRange textRange = new(txtRequest.Document.ContentStart, txtRequest.Document.ContentEnd);

                if (string.IsNullOrWhiteSpace(textRange.Text))
                {
                    MessageBox.Show("Please write a request.", EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await VS.StatusBar.ShowProgressAsync("Requesting chatGPT", 1, 2);

                string selectionFormated = TextFormat.FormatSelection(textRange.Text);

                txtResponse.Document = new();

                await ChatGPT.RequestAsync(options, selectionFormated, ResultHandler);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                MessageBox.Show(ex.Message, EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            txtRequest.Document = new();
        }

        /// <summary>
        /// Copy the content of the Response TextBox to the Clipboard.
        /// </summary>
        /// <param name="sender">The button that invokes the event.</param>
        /// <param name="e">Event arguments.</param>
        private void btnResponseCopy_Click(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new(txtResponse.Document.ContentStart, txtResponse.Document.ContentEnd);

            Clipboard.SetText(textRange.Text);
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

            btnRequestSend.IsEnabled = false;
            btnRequestPast.IsEnabled = false;
            btnRequestClear.IsEnabled = false;
            txtRequest.IsEnabled = false;
            txtResponse.IsEnabled = false;

            string message = "Please, set the OpenAI API key and restart Visual Studio.";

            TextRange textRange = new(txtRequest.Document.ContentEnd, txtRequest.Document.ContentEnd);

            textRange.Text = message;

            MessageBox.Show(message, EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

            package.ShowOptionPage(typeof(OptionPageGridGeneral));
        }

        /// <summary>
        /// Handles the result of an operation and appends it to the end of the txtResponse control.
        /// </summary>
        /// <param name="index">The index of the operation.</param>
        /// <param name="result">The result of the operation.</param>
        private async void ResultHandler(int index, CompletionResult result)
        {
            if (firstInteration)
            {
                await VS.StatusBar.ShowProgressAsync("Receiving chatGPT response", 2, 2);

                firstInteration = false;
            }

            TextPointer lastPosition = txtResponse.Document.ContentEnd.GetPositionAtOffset(-1);

            if (lastPosition != null)
            {
                TextRange textRange = new(lastPosition, txtResponse.Document.ContentEnd);

                if (textRange.Text == "\r\n")
                {
                    textRange.Text = String.Empty;
                }
            }

            txtResponse.AppendText(result.ToString());

            txtResponse.ScrollToEnd();
        }

        #endregion Methods
    }
}