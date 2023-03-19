using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
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
        #region Properties

        private OptionPageGridGeneral options;
        private Conversation chat;
        private List<ChatTurboItem> chatItems;

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
        public async void SendRequestAsync(Object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 1, 2);

                chatItems.Add(new ChatTurboItem(AuthorEnum.Me, txtRequest.Text));

                chat.AppendUserInput(txtRequest.Text);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    btnRequestSend.IsEnabled = false;
                    btnClear.IsEnabled = false;
                    txtRequest.Text = string.Empty;
                    chatList.Items.Refresh();
                    scrollViewer.ScrollToEnd();
                }));

                string response = await chat.GetResponseFromChatbot();

                chatItems.Add(new ChatTurboItem(AuthorEnum.ChatGPT, response));

                chatList.Items.Refresh();

                scrollViewer.ScrollToEnd();

                btnRequestSend.IsEnabled = true;
                btnClear.IsEnabled = true;

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_RECEIVING_CHATGPT, 2, 2);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                btnRequestSend.IsEnabled = true;
                btnClear.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the click event for the Clear button, which clears the conversation.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Clear the conversation?", Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            chat = ChatGPT.CreateConversation(options);
            chatItems.Clear();
            chatList.Items.Refresh();
        }

        /// <summary>
        /// This method changes the syntax highlighting of the textbox based on the language detected in the text.
        /// </summary>
        private void txtRequest_TextChanged(object sender, EventArgs e)
        {
            txtRequest.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition(TextFormat.DetectCodeLanguage(txtRequest.Text));
        }

        /// <summary>
        /// Handles the mouse wheel event for the text editor by scrolling the view.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The mouse wheel event arguments.</param>
        private void TextEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
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
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                MessageBox.Show(Constants.MESSAGE_SET_API_KEY_AND_RESTART, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                package.ShowOptionPage(typeof(OptionPageGridGeneral));

                return;
            }

            this.options = options;

            chat = ChatGPT.CreateConversation(options);

            chatItems = new();

            chatList.ItemsSource = chatItems;
        }

        #endregion Methods                    
    }
}