using JeffPires.VisualChatGPTStudio.Utils;using System.Windows;using System.Windows.Controls;using System.Windows.Input;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo{
    /// <summary>
    /// Represents a user control for displaying a chat item in a chat interface.
    /// </summary>
    public partial class ucChatItem : UserControl    {
        #region Properties
        private readonly TerminalWindowTurboControl parentControl;

        #endregion Properties
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ucChatItem class.
        /// </summary>
        /// <param name="parentControl">The parent TerminalWindowTurboControl.</param>
        /// <param name="chatName">The name of the chat item.</param>
        public ucChatItem(TerminalWindowTurboControl parentControl, string chatName)        {            this.InitializeComponent();            this.parentControl = parentControl;
            lblName.Text = chatName;        }

        #endregion Constructors
        #region Event Handlers

        /// <summary>
        /// Event handler for the click event of the delete image button.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The MouseEventArgs containing event data.</param>
        public void imgDelete_Click(object sender, MouseEventArgs e)        {            if (!imgDelete.IsEnabled)
            {
                return;
            }            if (MessageBox.Show($"Delete the chat \"{lblName.Text}\"?", Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)            {                parentControl.DeleteChat(this);            }        }

        /// <summary>
        /// Event handler for the click event of the imgEdit control.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The MouseEventArgs containing event data.</param>
        public void imgEdit_Click(object sender, MouseEventArgs e)        {            if (!imgEdit.IsEnabled)
            {
                return;
            }            txtName.Width = lblName.ActualWidth;            imgDelete.IsEnabled = false;            imgEdit.IsEnabled = false;            lblName.Visibility = Visibility.Collapsed;

            txtName.Text = lblName.Text;
            txtName.Visibility = Visibility.Visible;            txtName.Focus();            }

        /// <summary>
        /// Event handler for the PreviewKeyDown event of the txtName TextBox.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The KeyEventArgs containing information about the key that was pressed.</param>
        private void txtName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                txtName.Visibility = Visibility.Collapsed;
                lblName.Visibility = Visibility.Visible;

                imgDelete.IsEnabled = true;
                imgEdit.IsEnabled = true;
            }

            if (e.Key != Key.Enter)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("The name can not be null.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            string newName = txtName.Text.Trim();

            if (lblName.Text == newName)
            {
                return;
            }

            bool result = parentControl.SetChatNewName(this, newName);

            if (!result)
            {
                MessageBox.Show("Already have an item with this name.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            lblName.Text = newName;

            txtName.Visibility = Visibility.Collapsed;
            lblName.Visibility = Visibility.Visible;            imgDelete.IsEnabled = true;            imgEdit.IsEnabled = true;
        }

        /// <summary>
        /// Event handler for when the TxtName textbox loses focus. 
        /// It hides the TxtName textbox and shows the lblName label. 
        /// It also enables the imgDelete and imgEdit images.
        /// </summary>
        private void TxtName_LostFocus(object sender, RoutedEventArgs e)
        {
            txtName.Visibility = Visibility.Collapsed;
            lblName.Visibility = Visibility.Visible;            imgDelete.IsEnabled = true;            imgEdit.IsEnabled = true;
        }

        #endregion Event Handlers      
    }}