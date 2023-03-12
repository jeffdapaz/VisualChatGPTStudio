using System.Windows;
using System.Windows.Controls;

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

        private OptionPageGrid options;

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
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void button1_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion Event Handlers

        #region Methods

        /// <summary>
        /// Starts the control by checking if the OpenAI API key is set. If not, it shows a warning message and the option page.
        /// </summary>
        /// <param name="options">The options page.</param>
        /// <param name="package">The package.</param>
        public void StartControl(OptionPageGrid options, Package package)
        {
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                this.options = options;

                return;
            }

            string message = "Please, set the OpenAI API key and restart Visual Studio.";

            System.Windows.MessageBox.Show(message, EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

            package.ShowOptionPage(typeof(OptionPageGrid));
        }

        #endregion Methods
    }
}