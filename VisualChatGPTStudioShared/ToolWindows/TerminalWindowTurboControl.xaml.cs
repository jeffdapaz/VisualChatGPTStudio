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
using System.Windows.Media.Imaging;
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

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 1, 2);

                chatItems.Add(new ChatTurboItem(AuthorEnum.Me, txtRequest.Text, true, 0));

                chat.AppendUserInput(txtRequest.Text);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    btnRequestSend.IsEnabled = false;
                    btnClear.IsEnabled = false;
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
        /// Copies the text of the chat item at the given index to the clipboard.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">The event arguments.</param>
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int index = (int)button.Tag;

            Clipboard.SetText(chatItems[index].Document.Text);

            Image img = new() { Source = new BitmapImage(new Uri("pack://application:,,,/VisualChatGPTStudio;component/Resources/check.png")) };

            button.Content = img;
            button.ToolTip = "Copied!";

            System.Timers.Timer timer = new(2000) { Enabled = true };

            timer.Elapsed += (s, args) =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    img = new() { Source = new BitmapImage(new Uri("pack://application:,,,/VisualChatGPTStudio;component/Resources/copy.png")) };

                    button.Content = img;
                    button.ToolTip = "Copy code";
                }));

                timer.Enabled = false;
                timer.Dispose();
            };
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
        /// Starts the control with the given options and package.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="package">The package.</param>
        public void StartControl(OptionPageGridGeneral options, Package package)
        {
            this.options = options;
            this.package = package;

            chat = ChatGPT.CreateConversation(options);

            chatItems = new();

            chatList.ItemsSource = chatItems;
        }

        #endregion Methods                            
    }
}