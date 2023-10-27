using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        private CancellationTokenSource cancellationTokenSource;
        private DocumentView docView;
        private bool shiftKeyPressed;

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
        /// Handles the Click event of the btnRequestCode control.
        /// </summary>
        public async void SendCode(Object sender, ExecutedRoutedEventArgs e)
        {
            await RequestAsync(CommandType.Code);
        }

        /// <summary>
        /// Handles the Click event of the btnRequestSend control.
        /// </summary>
        public async void SendRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            await RequestAsync(CommandType.Request);
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public async void CancelRequest(Object sender, ExecutedRoutedEventArgs e)
        {
            EnableDisableButtons(true);

            cancellationTokenSource.Cancel();
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
        /// Sends a request asynchronously based on the command type.
        /// </summary>
        /// <param name="commandType">The type of command to execute.</param>
        private async System.Threading.Tasks.Task RequestAsync(CommandType commandType)
        {
            try
            {
                shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

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

                if (commandType == CommandType.Code)
                {
                    docView = await VS.Documents.GetActiveDocumentViewAsync();

                    string originalCode = docView.TextView.TextBuffer.CurrentSnapshot.GetText();

                    if (options.MinifyRequests)
                    {
                        originalCode = TextFormat.MinifyText(originalCode);
                    }

                    originalCode = TextFormat.RemoveCharactersFromText(originalCode, options.CharactersToRemoveFromRequests.Split(','));

                    chat.AppendSystemMessage(options.TurboChatCodeCommand);
                    chat.AppendUserInput(originalCode);
                }

                chatItems.Add(new ChatTurboItem(AuthorEnum.Me, txtRequest.Text, true, 0));

                string request = options.MinifyRequests ? TextFormat.MinifyText(txtRequest.Text) : txtRequest.Text;

                request = TextFormat.RemoveCharactersFromText(request, options.CharactersToRemoveFromRequests.Split(','));

                chat.AppendUserInput(request);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    EnableDisableButtons(false);
                    txtRequest.Text = string.Empty;
                    chatList.Items.Refresh();
                    scrollViewer.ScrollToEnd();
                }));

                string response = await SendRequestAsync();

                List<ChatTurboResponseSegment> segments = TurboChatHelper.GetChatTurboResponseSegments(response);

                AuthorEnum author;

                for (int i = 0; i < segments.Count; i++)
                {
                    author = segments[i].IsCode ? AuthorEnum.ChatGPTCode : AuthorEnum.ChatGPT;

                    if (author == AuthorEnum.ChatGPTCode && commandType == CommandType.Code && !shiftKeyPressed)
                    {
                        docView.TextView.TextBuffer.Replace(new Span(0, docView.TextView.TextBuffer.CurrentSnapshot.Length), segments[i].Content);
                    }
                    else
                    {
                        chatItems.Add(new ChatTurboItem(author, segments[i].Content, i == 0, chatItems.Count));
                    }
                }

                chatList.Items.Refresh();

                scrollViewer.ScrollToEnd();

                EnableDisableButtons(true);
            }
            catch (OperationCanceledException)
            {
                EnableDisableButtons(true);
            }
            catch (Exception ex)
            {
                EnableDisableButtons(true);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Sends a request to the chatbot asynchronously and waits for a response.
        /// </summary>
        /// <returns>The response from the chatbot.</returns>
        private async Task<string> SendRequestAsync()
        {
            cancellationTokenSource = new CancellationTokenSource();

            Task<string> task = chat.GetResponseFromChatbotAsync();

            await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));

            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            return await task;
        }

        /// <summary>
        /// Enables or disables the buttons based on the given boolean value.
        /// </summary>
        /// <param name="enable">Boolean value to enable or disable the buttons.</param>
        private void EnableDisableButtons(bool enable)
        {
            grdProgress.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;

            btnClear.IsEnabled = enable;
            btnRequestCode.IsEnabled = enable;
            btnRequestSend.IsEnabled = enable;
            btnCancel.IsEnabled = !enable;

            btnClear.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnRequestCode.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnRequestSend.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.Visibility = !enable ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion Methods                            
    }

    /// <summary>
    /// Represents the different types of commands that can be used.
    /// </summary>
    enum CommandType
    {
        Code = 0,
        Request = 1
    }
}