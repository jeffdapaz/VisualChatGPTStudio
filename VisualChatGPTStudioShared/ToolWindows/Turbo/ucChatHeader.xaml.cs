using System.Windows;using System.Windows.Controls;namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo{
    /// <summary>
    /// Represents a user control for the header section of a chat window.
    /// </summary>
    public partial class ucChatHeader : UserControl    {
        #region Properties
        private readonly TerminalWindowTurboControl parentControl;

        #endregion Properties
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ucChatHeader class.
        /// </summary>
        /// <param name="parentControl">The parent control of the ucChatHeader.</param>
        /// <param name="chatName">The header text to be displayed.</param>
        public ucChatHeader(TerminalWindowTurboControl parentControl, string chatName)        {            this.InitializeComponent();            this.parentControl = parentControl;            lblHeader.Text = chatName;        }

        #endregion Constructors
        #region Event Handlers
        /// <summary>
        /// Event handler for the btnClose button click event. Closes the current tab by calling the CloseTab method of the parent control.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnClose_Click(object sender, RoutedEventArgs e)        {            parentControl.CloseTab(this);        }

        #endregion Event Handlers       
        #region Methods
        /// <summary>
        /// Updates the text of the header label with the specified value.
        /// </summary>
        /// <param name="chatName">The new text for the header label.</param>
        public void UpdateChatName(string chatName)        {            lblHeader.Text = chatName;        }

        #endregion Methods                                   }}