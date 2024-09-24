using Community.VisualStudio.Toolkit;
using ICSharpCode.AvalonEdit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private readonly ConversationOverride chat;
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

            rowRequest.MaxHeight = parentControl.ActualHeight - 200;
            txtRequest.MaxHeight = rowRequest.MaxHeight - 10;

            StringBuilder segments;

            chat = ChatGPT.CreateConversation(options, options.TurboChatBehavior);

            chatListControlItems = [];

            foreach (MessageEntity message in messages.OrderBy(m => m.Order))
            {
                firstMessage = false;
                segments = new();

                message.Segments = message.Segments.OrderBy(s => s.SegmentOrderStart).ToList();

                for (int i = 0; i < message.Segments.Count; i++)
                {
                    segments.AppendLine(message.Segments[i].Content);
                }

                chatListControlItems.Add(new ChatListControlItem(message.Segments[0].Author, segments.ToString()));

                chat.AppendUserInput(segments.ToString());
            }

            chatList.ItemsSource = chatListControlItems;

            scrollViewer.ScrollToEnd();
        }

        #endregion Constructors

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the btnRequestCode control.
        /// </summary>
        public async void SendCode(object sender, RoutedEventArgs e)
        {
            await RequestAsync(CommandType.Code);
        }

        /// <summary>
        /// Handles the Click event of the btnRequestSend control.
        /// </summary>
        public async void SendRequest(object sender, RoutedEventArgs e)
        {
            await RequestAsync(CommandType.Request);
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public async void CancelRequest(object sender, RoutedEventArgs e)
        {
            EnableDisableButtons(true);

            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Handles the PreviewMouseWheel event for the txtChat control, scrolling the associated ScrollViewer based on the mouse wheel delta.
        /// </summary>
        private void txtChat_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                MdXaml.MarkdownScrollViewer mdXaml = (MdXaml.MarkdownScrollViewer)sender;

                List<TextEditor> textEditors = FindMarkDownCodeTextEditors(mdXaml);

                if (textEditors != null)
                {
                    foreach (TextEditor textEditor in textEditors)
                    {
                        ScrollViewer scrollViewerEditor = textEditor.Template.FindName("PART_ScrollViewer", textEditor) as ScrollViewer;

                        if (scrollViewerEditor != null)
                        {
                            scrollViewerEditor.ScrollToHorizontalOffset(scrollViewerEditor.HorizontalOffset - e.Delta);
                        }
                    }
                }
            }
            else
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            }

            e.Handled = true;
        }

        /// <summary>
        /// To avoid the behavior that caused the scroll to move automatically when clicking with the mouse to select text
        /// </summary>
        private void txtChat_GotFocus(object sender, RoutedEventArgs e)
        {
            double currentOffset = scrollViewer.VerticalOffset;

            scrollViewer.Opacity = 0.5;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                scrollViewer.ScrollToVerticalOffset(currentOffset);
                scrollViewer.Opacity = 1;
            }), System.Windows.Threading.DispatcherPriority.Background);
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

                if (!options.UseVisualStudioIdentity && string.IsNullOrWhiteSpace(options.ApiKey))
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

                chatListControlItems.Add(new ChatListControlItem(AuthorEnum.Me, txtRequest.Text));

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

                messages.Add(new() { Order = messages.Count + 1, Segments = [new() { Author = AuthorEnum.Me, Content = request }] });

                if (commandType == CommandType.Code && !shiftKeyPressed)
                {
                    List<ChatMessageSegment> segments = TextFormat.GetChatTurboResponseSegments(response);

                    for (int i = 0; i < segments.Count; i++)
                    {
                        if (segments[i].Author == AuthorEnum.ChatGPTCode)
                        {
                            docView.TextView.TextBuffer.Replace(new Microsoft.VisualStudio.Text.Span(0, docView.TextView.TextBuffer.CurrentSnapshot.Length), segments[i].Content);
                        }
                        else
                        {
                            chatListControlItems.Add(new ChatListControlItem(segments[i].Author, segments[i].Content));
                        }
                    }
                }
                else
                {
                    messages.Add(new() { Order = messages.Count + 1, Segments = [new() { Author = AuthorEnum.ChatGPT, Content = response }] });
                    chatListControlItems.Add(new ChatListControlItem(AuthorEnum.ChatGPT, response));
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

        /// <summary>
        /// Recursively searches for all TextEditor controls within the visual tree of a given DependencyObject.
        /// </summary>
        /// <param name="parent">The parent DependencyObject to start the search from.</param>
        /// <returns>
        /// A list of all found TextEditor controls.
        /// </returns>
        public static List<TextEditor> FindMarkDownCodeTextEditors(DependencyObject parent)
        {
            List<TextEditor> foundChildren = [];

            if (parent == null)
            {
                return foundChildren;
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                TextEditor childType = child as TextEditor;

                if (childType == null)
                {
                    foundChildren.AddRange(FindMarkDownCodeTextEditors(child));
                }
                else
                {
                    foundChildren.Add(childType);
                }
            }

            return foundChildren;
        }

        #endregion Methods                            
    }
}