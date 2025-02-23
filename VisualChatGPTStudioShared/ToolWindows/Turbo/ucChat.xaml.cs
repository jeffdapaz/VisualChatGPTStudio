using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Agents;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using JeffPires.VisualChatGPTStudio.Utils.API;
using JeffPires.VisualChatGPTStudio.Utils.CodeCompletion;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using OpenAI_API.Chat;
using OpenAI_API.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisualChatGPTStudioShared.ToolWindows.Turbo;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using MessageBox = System.Windows.MessageBox;
using TextEditor = ICSharpCode.AvalonEdit.TextEditor;

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
        private readonly CancellationTokenSource cancellationTokenSource;
        private DocumentView docView;
        private bool shiftKeyPressed;
        private bool selectedContextFilesCodeAppended = false;
        private bool firstMessage = true;
        private readonly CompletionManager completionManager;
        private byte[] attachedImage;

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
            //It is necessary for the MdXaml library load successfully
            MdXaml.MarkdownScrollViewer _ = new();

            this.InitializeComponent();

            this.parentControl = parentControl;
            this.options = options;
            this.package = package;
            this.ChatHeader = ucChatHeader;
            this.messages = messages;

            rowRequest.MaxHeight = parentControl.ActualHeight - 200;
            txtRequest.MaxHeight = rowRequest.MaxHeight - 10;

            txtRequest.TextArea.TextEntering += txtRequest_TextEntering;
            txtRequest.TextArea.TextEntered += txtRequest_TextEntered;
            txtRequest.PreviewKeyDown += AttachImage.TextEditor_PreviewKeyDown;

            AttachImage.OnImagePaste += AttachImage_OnImagePaste;

            completionManager = new CompletionManager(package, txtRequest);

            StringBuilder segments;

            chat = ApiHandler.CreateConversation(options, options.TurboChatBehavior);

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
        /// Handles the text entered event for the request text box, 
        /// passing the entered text to the CompletionManager for processing.
        /// </summary>
        /// <param name="sender">The source of the event, typically the text box.</param>
        /// <param name="e">The event data containing the text that was entered.</param>
        private async void txtRequest_TextEntered(object sender, TextCompositionEventArgs e)
        {
            await completionManager.HandleTextEnteredAsync(e);
        }

        /// <summary>
        /// Handles the text entering event for the request text box, delegating the processing to the CompletionManager.
        /// </summary>
        /// <param name="sender">The source of the event, typically the text box.</param>
        /// <param name="e">The event data containing information about the text composition.</param>
        private void txtRequest_TextEntering(object sender, TextCompositionEventArgs e)
        {
            completionManager.HandleTextEntering(e);
        }

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

            cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Handles the click event of the button to attach an image. 
        /// Opens a file dialog to select an image file, validates the file extension, 
        /// and reads the selected image file into a byte array if valid.
        /// </summary>
        private void btnAttachImage_Click(object sender, RoutedEventArgs e)
        {
            if (AttachImage.ShowDialog(out attachedImage, out string imageName))
            {
                txtImage.Text = imageName;

                txtImage.Visibility = Visibility.Visible;
                btnDeleteImage.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Handles the click event for the delete image button. 
        /// Hides the image display and the attach image button, 
        /// and clears the attached image reference.
        /// </summary>
        private void btnDeleteImage_Click(object sender, RoutedEventArgs e)
        {
            txtImage.Visibility = Visibility.Hidden;
            btnDeleteImage.Visibility = Visibility.Hidden;

            attachedImage = null;
        }

        /// <summary>
        /// Handles the SQL button click event and populates the connection dropdown.
        /// </summary>
        private async void btnSql_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                cbConnection.ItemsSource = SqlServerAgent.GetConnections();

                cbConnection.SelectedIndex = 0;

                grdCommands.Visibility = Visibility.Collapsed;
                grdSQL.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Handles the SQL send button click event. Retrieves the database schema, processes SQL functions, 
        /// and sends a request asynchronously. Displays error messages and toggles UI visibility in case of exceptions.
        /// </summary>
        private async void btnSqlSend_Click(object sender, RoutedEventArgs e)
        {
            string dataBaseSchema;

            try
            {
                dataBaseSchema = SqlServerAgent.GetDataBaseSchema(cbConnection.SelectedValue.ToString());
            }
            catch (Exception ex)
            {
                EnableDisableButtons(true);

                grdSQL.Visibility = Visibility.Collapsed;
                grdCommands.Visibility = Visibility.Visible;

                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return;
            }

            List<FunctionRequest> sqlFunctions = SqlServerAgent.GetSqlFunctions();

            foreach (FunctionRequest function in sqlFunctions)
            {
                chat.AppendFunctionCall(function);
            }

            string request = options.SqlServerAgentCommand + Environment.NewLine + "Database: " + dataBaseSchema + Environment.NewLine;

            string requestToShowOnList = options.SqlServerAgentCommand + Environment.NewLine + Environment.NewLine + ((SqlServerConnectionInfo)cbConnection.SelectedItem).Description;

            await RequestAsync(CommandType.Request, request, requestToShowOnList, false);

            grdSQL.Visibility = Visibility.Collapsed;
            grdCommands.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles the click event for the SQL Cancel button. Cancels the current request and toggles the visibility of the SQL and Commands grids.
        /// </summary>
        private async void btnSqlCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelRequest(sender, e);

            grdSQL.Visibility = Visibility.Collapsed;
            grdCommands.Visibility = Visibility.Visible;
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

        /// <summary>
        /// Handles the event when an image is pasted, attaching the image and updating the UI with the file name.
        /// </summary>
        /// <param name="attachedImage">The byte array representing the pasted image.</param>
        /// <param name="fileName">The name of the pasted image file.</param>
        private void AttachImage_OnImagePaste(byte[] attachedImage, string fileName)
        {
            this.attachedImage = attachedImage;

            txtImage.Text = fileName;

            txtImage.Visibility = Visibility.Visible;
            btnDeleteImage.Visibility = Visibility.Visible;
        }

        #endregion Event Handlers

        #region Methods        

        /// <summary>
        /// Handles an asynchronous request based on the specified command type. Validates input, appends context or system messages, processes code or image-related requests, and sends the final request.
        /// </summary>
        private async System.Threading.Tasks.Task RequestAsync(CommandType commandType)
        {
            shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (!options.AzureEntraIdAuthentication && string.IsNullOrWhiteSpace(options.ApiKey))
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

            string requestToShowOnList = txtRequest.Text;

            if (attachedImage != null)
            {
                requestToShowOnList = "🖼️ " + txtImage.Text + Environment.NewLine + Environment.NewLine + requestToShowOnList;

                List<ChatContentForImage> chatContent = [new(attachedImage)];

                chat.AppendUserInput(chatContent);
            }

            string request = await completionManager.ReplaceReferencesAsync(txtRequest.Text);

            txtRequest.Text = string.Empty;

            await RequestAsync(commandType, request, requestToShowOnList, shiftKeyPressed);
        }

        /// <summary>
        /// Sends an asynchronous request based on the provided command type, processes the response, 
        /// and updates the UI elements accordingly. Handles exceptions and manages UI state during the operation.
        /// </summary>
        private async System.Threading.Tasks.Task RequestAsync(CommandType commandType, string request, string requestToShowOnList, bool shiftKeyPressed)
        {
            try
            {
                chatListControlItems.Add(new ChatListControlItem(AuthorEnum.Me, requestToShowOnList));

                request = options.MinifyRequests ? TextFormat.MinifyText(request) : request;

                request = TextFormat.RemoveCharactersFromText(request, options.CharactersToRemoveFromRequests.Split(','));

                chat.AppendUserInput(request);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    EnableDisableButtons(false);
                    chatList.Items.Refresh();
                    scrollViewer.ScrollToEnd();
                }));

                messages.Add(new() { Order = messages.Count + 1, Segments = [new() { Author = AuthorEnum.Me, Content = requestToShowOnList }] });

                CancellationTokenSource cancellationToken = new();

                (string, List<FunctionResult>) result = await SendRequestAsync(cancellationToken);

                if (result.Item2 != null && result.Item2.Any())
                {
                    await HandleFunctionsCallsAsync(result.Item2, cancellationToken);
                }
                else
                {
                    HandleResponse(commandType, shiftKeyPressed, result.Item1);
                }

                EnableDisableButtons(true);

                if (firstMessage)
                {
                    await UpdateHeaderAsync(cancellationToken);
                }
                else
                {
                    parentControl.NotifyNewChatMessagesAdded(ChatHeader, messages);
                }
            }
            catch (OperationCanceledException)
            {
                //catch request cancelation
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                EnableDisableButtons(true);

                txtImage.Visibility = Visibility.Hidden;
                btnDeleteImage.Visibility = Visibility.Hidden;

                attachedImage = null;
            }
        }

        /// <summary>
        /// Sends an asynchronous request and waits for the response or cancellation. 
        /// If the operation is canceled, a cancellation exception is thrown. 
        /// Returns a tuple containing a string and a list of FunctionResult objects upon successful completion.
        /// </summary>
        /// <returns>
        /// A tuple with a string and a list of FunctionResult objects.
        /// </returns>
        private async Task<(string, List<FunctionResult>)> SendRequestAsync(CancellationTokenSource cancellationTokenSource)
        {
            Task<(string, List<FunctionResult>)> task = chat.GetResponseContentAndFunctionAsync();

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

            btnAttachImage.IsEnabled = enable;
            btnRequestCode.IsEnabled = enable;
            btnRequestSend.IsEnabled = enable;
            btnSqlSend.IsEnabled = enable;
            btnCancel.IsEnabled = !enable;

            btnAttachImage.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
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
        /// Updates the chat header asynchronously by generating a concise and relevant title for the first message 
        /// based on its context, limited to three words, and notifies the parent control of the new chat creation.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        private async System.Threading.Tasks.Task UpdateHeaderAsync(CancellationTokenSource cancellationToken)
        {
            string request = "Please suggest a concise and relevant title for my first message based on its context, using up to three words and in the same language as my first message.";

            chat.AppendUserInput(request);

            (string, List<FunctionResult>) result = await SendRequestAsync(cancellationToken);

            string chatName = result.Item1;

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
        private static List<TextEditor> FindMarkDownCodeTextEditors(DependencyObject parent)
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

        /// <summary>
        /// Handles the response based on the command type and shift key state, updating the document view or chat list control items accordingly.
        /// </summary>
        private void HandleResponse(CommandType commandType, bool shiftKeyPressed, string response)
        {
            if (commandType == CommandType.Code && !shiftKeyPressed)
            {
                List<ChatMessageSegment> segments = TextFormat.GetChatTurboResponseSegments(response);

                for (int i = 0; i < segments.Count; i++)
                {
                    if (segments[i].Author == AuthorEnum.ChatGPTCode)
                    {
                        docView.TextView.TextBuffer.Replace(new Span(0, docView.TextView.TextBuffer.CurrentSnapshot.Length), segments[i].Content);
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
        }

        private async System.Threading.Tasks.Task<bool> HandleFunctionsCallsAsync(List<FunctionResult> functions, CancellationTokenSource cancellationToken)
        {
            string functionResult;

            foreach (FunctionResult function in functions)
            {
                functionResult = SqlServerAgent.ExecuteFunction((List<SqlServerConnectionInfo>)cbConnection.ItemsSource, function, out List<Dictionary<string, object>> readerResult);

                chat.AppendToolMessage(function.Id, functionResult);
            }

            (string, List<FunctionResult>) result = await SendRequestAsync(cancellationToken);

            bool responseHandled = false;

            if (result.Item2 != null && result.Item2.Any())
            {
                responseHandled = await HandleFunctionsCallsAsync(result.Item2, cancellationToken);
            }

            if (!responseHandled)
            {
                HandleResponse(CommandType.Request, false, result.Item1);

                responseHandled = true;
            }

            return responseHandled;
        }

        #endregion Methods                            
    }
}