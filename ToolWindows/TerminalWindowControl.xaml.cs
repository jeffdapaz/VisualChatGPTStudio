using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.VisualStudio.PlatformUI;
using OpenAI_API.Completions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
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

            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

            SetBoxesColor();
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

                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    MessageBox.Show("Please write a request.", EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await VS.StatusBar.ShowProgressAsync("Requesting chatGPT", 1, 2);

                string selectionFormated = TextFormat.FormatSelection(txtRequest.Text);

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
            Clipboard.SetText(txtResponse.Text);
        }

        /// <summary>
        /// This method changes the syntax highlighting of the textbox based on the language detected in the text.
        /// </summary>
        private void txtRequest_TextChanged(object sender, EventArgs e)
        {
            txtRequest.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition(TextFormat.DetectCodeLanguage(txtRequest.Text));
        }

        /// <summary>
        /// Sets the color of the boxes when the theme is changed.
        /// </summary>
        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            SetBoxesColor();
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

            txtRequest.Text = message;

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

            txtResponse.AppendText(result.ToString());

            txtResponse.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition(TextFormat.DetectCodeLanguage(txtResponse.Text));

            txtResponse.ScrollToEnd();
        }

        /// <summary>
        /// Sets the background color of the text boxes based on the current Visual Studio color theme.
        /// </summary>
        private void SetBoxesColor()
        {
            System.Drawing.Color currentColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);

            SolidColorBrush newcolor;

            if (currentColor == System.Drawing.Color.FromArgb(255, 31, 31, 31))
            {
                newcolor = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            }
            else
            {
                newcolor = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }

            txtRequest.Background = newcolor;
            txtResponse.Background = newcolor;
        }

        #endregion Methods        
    }
}