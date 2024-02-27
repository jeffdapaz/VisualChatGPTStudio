using Community.VisualStudio.Toolkit;
using ICSharpCode.AvalonEdit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualChatGPTStudioShared.ToolWindows.Turbo;
using MessageBox = System.Windows.MessageBox;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    /// <summary>
    /// Interaction logic for TerminalWindowTurboControl.
    /// </summary>
    public partial class ucChat : UserControl
    {
        #region Properties

        public ucChatHeader ChatHeader { get; private set; }

        private readonly TerminalWindowTurboControl parentControl;
        private readonly OptionPageGridGeneral options;
        private readonly Package package;
        private readonly List<MessageEntity> messages;
        private readonly Conversation chat;
        private readonly List<ChatListControlItem> chatListControlItems;
        private CancellationTokenSource cancellationTokenSource;
        private DocumentView docView;
        private bool shiftKeyPressed;
        private bool selectedContextFilesCodeAppended = false;
        private bool firstMessage = true;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ucChat class.
        /// </summary>
        /// <param name="parentControl">The parent TerminalWindowTurboControl.</param>
        /// <param name="options">The OptionPageGridGeneral options.</param>
        /// <param name="package">The Package package.</param>
        /// <param name="ucChatHeader">The ucChatHeader control.</param>
        /// <param name="messages">The list of MessageEntity messages.</param>
        public ucChat(TerminalWindowTurboControl parentControl, OptionPageGridGeneral options, Package package, ucChatHeader ucChatHeader, List<MessageEntity> messages)
        {
            this.InitializeComponent();

            this.parentControl = parentControl;
            this.options = options;
            this.package = package;
            this.ChatHeader = ucChatHeader;
            this.messages = messages;

            StringBuilder segments;

            chat = ChatGPT.CreateConversation(options, options.TurboChatBehavior);

            chatListControlItems = new();

            foreach (MessageEntity message in messages.OrderBy(m => m.Order))
            {
                firstMessage = false;
                segments = new();

                message.Segments = message.Segments.OrderBy(s => s.SegmentOrderStart).ToList();

                for (int i = 0; i < message.Segments.Count; i++)
                {
                    chatListControlItems.Add(new ChatListControlItem(message.Segments[i].Author, message.Segments[i].Content, i == 0, i == message.Segments.Count - 1, chatListControlItems.Count));

                    segments.AppendLine(message.Segments[i].Content);
                }

                chat.AppendUserInput(segments.ToString());
            }

            chatList.ItemsSource = chatListControlItems;
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
        /// Copies the text of the chat item at the given index to the clipboard.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">The event arguments.</param>
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Image button = (Image)sender;

            int index = (int)button.Tag;

            TerminalWindowHelper.Copy(button, chatListControlItems[index].Document.Text);
        }

        /// <summary>
        /// Handles the mouse wheel event for the text editor by scrolling the view.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The mouse wheel event arguments.</param>
        private void TextEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            TextEditor editor = (TextEditor)sender;

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                ScrollViewer scrollViewerEditor = editor.Template.FindName("PART_ScrollViewer", editor) as ScrollViewer;

                scrollViewerEditor.ScrollToHorizontalOffset(scrollViewerEditor.HorizontalOffset - e.Delta);
            }
            else
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            }

            e.Handled = true;
        }

        #endregion Event Handlers

        #region Methods        

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

                if (!selectedContextFilesCodeAppended)
                {
                    string selectedContextFilesCode = await GetSelectedContextItemsCodeAsync();

                    if (!string.IsNullOrWhiteSpace(selectedContextFilesCode))
                    {
                        chat.AppendSystemMessage(selectedContextFilesCode);
                        selectedContextFilesCodeAppended = true;
                    }
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

                chatListControlItems.Add(new ChatListControlItem(AuthorEnum.Me, txtRequest.Text, true, true, 0));

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

                AddMessageSegments(new() { new() { Author = AuthorEnum.Me, Content = request } });

                List<ChatMessageSegment> segments = TextFormat.GetChatTurboResponseSegments(response);

                AddMessageSegments(segments);

                for (int i = 0; i < segments.Count; i++)
                {
                    if (segments[i].Author == AuthorEnum.ChatGPTCode && commandType == CommandType.Code && !shiftKeyPressed)
                    {
                        docView.TextView.TextBuffer.Replace(new Span(0, docView.TextView.TextBuffer.CurrentSnapshot.Length), segments[i].Content);
                    }
                    else
                    {
                        chatListControlItems.Add(new ChatListControlItem(segments[i].Author, segments[i].Content, i == 0, i == segments.Count - 1, chatListControlItems.Count));
                    }
                }

                chatList.Items.Refresh();

                scrollViewer.ScrollToEnd();

                EnableDisableButtons(true);

                if (firstMessage)
                {
                    await UpdateHeaderAsync();
                }
                else
                {
                    parentControl.NotifyNewChatMessagesAdded(ChatHeader, messages);
                }
            }
            catch (OperationCanceledException)
            {
                EnableDisableButtons(true);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

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

            btnRequestCode.IsEnabled = enable;
            btnRequestSend.IsEnabled = enable;
            btnCancel.IsEnabled = !enable;

            btnRequestCode.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnRequestSend.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.Visibility = !enable ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Asynchronously gets the code of the selected context items.
        /// </summary>  
        /// <returns>The code of the selected context items as a string.</returns>
        private static async Task<string> GetSelectedContextItemsCodeAsync()
        {
            StringBuilder result = new();

            List<string> selectedContextFilesCode = await TerminalWindowSolutionContextCommand.Instance.GetSelectedContextItemsCodeAsync();

            foreach (string code in selectedContextFilesCode)
            {
                result.AppendLine(code);
            }

            return result.ToString();
        }

        /// <summary>
        /// Adds a list of chat message segments to the existing messages list.
        /// </summary>
        /// <param name="segments">The list of chat message segments to be added.</param>
        private void AddMessageSegments(List<ChatMessageSegment> segments)
        {
            int order = messages.Count + 1;

            messages.Add(new() { Order = order, Segments = segments });
        }

        /// <summary>
        /// Updates the header of the chat.
        /// </summary>
        private async System.Threading.Tasks.Task UpdateHeaderAsync()
        {
            string request = "Please suggest a concise and relevant title for my first message based on its context, using up to three words and in the same language as my first message.";

            chat.AppendUserInput(request);

            string chatName = await SendRequestAsync();

            chatName = TextFormat.RemoveCharactersFromText(chatName, "\r\n", "\n", "\r", ".", ",", ":", ";", "'", "\"");

            string[] words = chatName.Split(' ');

            if (words.Length > 3)
            {
                chatName = string.Concat(words[0], " ", words[1]);
            }

            ChatHeader.UpdateChatName(chatName);

            parentControl.NotifyNewChatCreated(ChatHeader, chatName, messages);

            firstMessage = false;
        }

        #endregion Methods                            
    }
}