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
        private Package package;
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
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    MessageBox.Show(Constants.MESSAGE_SET_API_KEY, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                    package.ShowOptionPage(typeof(OptionPageGridGeneral));

                    return;
                }

                if (string.IsNullOrWhiteSpace(txtRequest.Text))
                {
                    MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string request = options.MinifyRequests ? TextFormat.MinifyText(txtRequest.Text) : txtRequest.Text;

                request = TextFormat.RemoveCharactersFromText(request, options.CharactersToRemoveFromRequests.Split(','));

                chatItems.Add(new ChatTurboItem(AuthorEnum.Me, txtRequest.Text, true, 0));

                chat.AppendUserInput(txtRequest.Text);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    EnableDisableButtons(false);
                    txtRequest.Text = string.Empty;
                    chatList.Items.Refresh();
                    scrollViewer.ScrollToEnd();
                }));

                string response = await chat.GetResponseFromChatbotAsync();

                List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments(response);

                AuthorEnum author;

                for (int i = 0; i < segments.Count; i++)
                {
                    author = segments[i].IsCode ? AuthorEnum.ChatGPTCode : AuthorEnum.ChatGPT;

                    chatItems.Add(new ChatTurboItem(author, segments[i].Content, i == 0, chatItems.Count));
                }

                chatList.Items.Refresh();

                scrollViewer.ScrollToEnd();

                EnableDisableButtons(true);
            }
            catch (Exception ex)
            {
                EnableDisableButtons(true);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
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

            chat = ChatGPT.CreateConversation(options, options.TurboChatBehavior);

            chatItems.Clear();
            chatList.Items.Refresh();
        }

        /// <summary>
        /// Copies the text of the chat item at the given index to the clipboard.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">The event arguments.</param>
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int index = (int)button.Tag;

            TerminalWindowHelper.Copy(button, chatItems[index].Document.Text);
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
        /// Starts the control with the given options and package.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="package">The package.</param>
        public void StartControl(OptionPageGridGeneral options, Package package)
        {
            this.options = options;
            this.package = package;

            chat = ChatGPT.CreateConversation(options, options.TurboChatBehavior);

            chatItems = new();

            chatList.ItemsSource = chatItems;
        }

        /// <summary>
        /// Enables or disables the buttons based on the given boolean value.
        /// </summary>
        /// <param name="enable">Boolean value to enable or disable the buttons.</param>
        private void EnableDisableButtons(bool enable)
        {
            grdProgress.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;

            btnClear.IsEnabled = enable;
            btnRequestSend.IsEnabled = enable;
        }

        #endregion Methods                            
    }
}