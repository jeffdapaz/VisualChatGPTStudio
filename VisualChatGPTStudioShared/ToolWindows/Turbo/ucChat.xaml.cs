using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Agents;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using JeffPires.VisualChatGPTStudio.Utils.API;
using JeffPires.VisualChatGPTStudio.Utils.CodeCompletion;
using JeffPires.VisualChatGPTStudio.Utils.Repositories;
using Markdig;
using Markdig.SyntaxHighlighting;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using OpenAI_API.Chat;
using OpenAI_API.Functions;
using OpenAI_API.ResponsesAPI.Models.Request;
using OpenAI_API.ResponsesAPI.Models.Response;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VisualChatGPTStudioShared.Agents.ApiAgent;
using VisualChatGPTStudioShared.ToolWindows.Turbo;
using Color = System.Windows.Media.Color;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using MessageBox = System.Windows.MessageBox;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    /// <summary>
    /// Interaction logic for TerminalWindowTurboControl.
    /// </summary>
    public partial class ucChat : UserControl
    {
        #region Constants

        private const string TAG_IMG = "#IMG#";
        private const string TAG_SQL = "#SQL#";
        private const string TAG_API = "#API#";

        #endregion Constants

        #region Properties

        public ucChatHeader ChatHeader { get; private set; }

        private readonly TerminalWindowTurboControl parentControl;
        private readonly OptionPageGridGeneral options;
        private readonly Package package;
        private readonly List<MessageEntity> messagesForDatabase;
        private readonly Conversation apiChat;
        private CancellationTokenSource cancellationTokenSource;
        private readonly string chatId;
        private DocumentView docView;
        private bool shiftKeyPressed;
        private bool selectedContextFilesCodeAppended = false;
        private bool firstMessage = true;
        private readonly CompletionManager completionManager;
        private byte[] attachedImage;
        private List<SqlServerConnectionInfo> sqlServerConnections;
        private List<ApiItem> apiDefinitions;
        private readonly List<string> sqlServerConnectionsAlreadyAdded = [];
        private readonly List<string> apiDefinitionsAlreadyAdded = [];
        private readonly MarkdownPipeline markdownPipeline;
        private readonly StringBuilder messagesHtml = new();
        private readonly Dictionary<IdentifierEnum, string> base64Images = [];
        private Rectangle screenBounds;
        private string previousResponseId;
        private readonly string meIcon;
        private readonly string chatGptIcon;
        private readonly string apiIcon;
        private readonly string copyIcon;
        private readonly string checkIcon;
        private readonly string sqlIcon;
        private readonly string imgIcon;

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
        /// <param name="chatId">The database chat id.</param>
        public ucChat(TerminalWindowTurboControl parentControl,
                      OptionPageGridGeneral options,
                      Package package,
                      ucChatHeader ucChatHeader,
                      List<MessageEntity> messages,
                      string chatId)
        {
            this.InitializeComponent();

            this.parentControl = parentControl;
            this.options = options;
            this.package = package;
            this.ChatHeader = ucChatHeader;
            this.messagesForDatabase = messages;
            this.chatId = chatId;

            meIcon = GetImageBase64(IdentifierEnum.Me);
            chatGptIcon = GetImageBase64(IdentifierEnum.ChatGPT);
            apiIcon = GetImageBase64(IdentifierEnum.Api);
            copyIcon = GetImageBase64(IdentifierEnum.CopyIcon);
            checkIcon = GetImageBase64(IdentifierEnum.CheckIcon);
            sqlIcon = GetImageBase64(IdentifierEnum.SqlIcon);
            imgIcon = GetImageBase64(IdentifierEnum.ImgIcon);

            rowRequest.MaxHeight = parentControl.ActualHeight - 200;
            txtRequest.MaxHeight = rowRequest.MaxHeight - 10;

            txtRequest.TextArea.TextEntering += txtRequest_TextEntering;
            txtRequest.TextArea.TextEntered += txtRequest_TextEntered;
            txtRequest.PreviewKeyDown += AttachImage.TextEditor_PreviewKeyDown;

            AttachImage.OnImagePaste += AttachImage_OnImagePaste;

            completionManager = new CompletionManager(package, txtRequest);

            markdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().DisableHtml().UseSyntaxHighlighting().Build();

            StringBuilder segments;

            apiChat = ApiHandler.CreateConversation(options, options.TurboChatBehavior);

            foreach (MessageEntity message in messages.OrderBy(m => m.Order))
            {
                firstMessage = false;
                segments = new();

                message.Segments = message.Segments.OrderBy(s => s.SegmentOrderStart).ToList();

                for (int i = 0; i < message.Segments.Count; i++)
                {
                    segments.AppendLine(message.Segments[i].Content);
                }

                if (message.Segments[0].Author == IdentifierEnum.FunctionCall)
                {
                    apiChat.AppendFunctionCall(JsonConvert.DeserializeObject<FunctionRequest>(message.Segments[0].Content));
                }
                else if (message.Segments[0].Author == IdentifierEnum.FunctionRequest)
                {
                    apiChat.AppendUserInput(message.Segments[0].Content);
                }
                else if (message.Segments[0].Author == IdentifierEnum.Api)
                {
                    AddMessagesHtml(message.Segments[0].Author, segments.ToString());
                }
                else
                {
                    AddMessagesHtml(message.Segments[0].Author, segments.ToString());

                    apiChat.AppendUserInput(segments.ToString());
                }
            }

            UpdateBrowser();

            sqlServerConnectionsAlreadyAdded = ChatRepository.GetSqlServerConnections(chatId);
            apiDefinitionsAlreadyAdded = ChatRepository.GetApiDefinitions(chatId);
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
        /// Handles the click event of the btnComputerUse button. 
        /// Captures a screenshot of the focused screen, sends a request asynchronously to the API with the captured image and user input.
        /// </summary>
        private async void btnComputerUse_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteRequestWithCommonHandlingAsync(async () =>
            {
                if (!IsReadyToSendRequest())
                {
                    return;
                }

                EnableDisableButtons(false, true);

                byte[] screenshot = ScreenCapturer.CaptureFocusedScreenScreenshot(out screenBounds);

                string request = txtRequest.Text;

                txtRequest.Text = string.Empty;

                AddMessagesHtml(IdentifierEnum.Me, request);

                UpdateBrowser();

                messagesForDatabase.Add(new() { Order = messagesForDatabase.Count + 1, Segments = [new() { Author = IdentifierEnum.Me, Content = request }] });

                cancellationTokenSource = new();

                Task<ComputerUseResponse> task;

                if (!string.IsNullOrWhiteSpace(previousResponseId))
                {
                    task = ApiHandler.GetComputerUseResponseAsync(options, request, screenBounds.Width, screenBounds.Height, previousResponseId, cancellationTokenSource.Token);
                }
                else
                {
                    task = ApiHandler.GetComputerUseResponseAsync(options, request, screenBounds.Width, screenBounds.Height, screenshot, cancellationTokenSource.Token);
                }

                await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));

                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                ComputerUseResponse response = await task;

                await ProcessComputerUseResponseAsync(response);
            });
        }

        /// <summary>
        /// Handles the Click event of the btnRequestCode control.
        /// </summary>
        public async void SendCode(object sender, RoutedEventArgs e)
        {
            await RequestAsync(RequestType.Code);
        }

        /// <summary>
        /// Handles the Click event of the btnRequestSend control.
        /// </summary>
        public async void SendRequest(object sender, RoutedEventArgs e)
        {
            await RequestAsync(RequestType.Request);
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

                spImage.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Handles the click event for the delete image button. 
        /// Hides the image display and the attach image button, 
        /// and clears the attached image reference.
        /// </summary>
        private void btnDeleteImage_Click(object sender, RoutedEventArgs e)
        {
            spImage.Visibility = Visibility.Collapsed;

            attachedImage = null;
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

            spImage.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles the PreviewMouseWheel event for a DataGrid to enable horizontal scrolling when the Shift key is pressed.
        /// </summary>
        private void DataGridResult_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                ScrollViewer scrollViewer = FindVisualChild<ScrollViewer>(sender as DataGrid);

                if (scrollViewer != null)
                {
                    if (e.Delta > 0)
                    {
                        scrollViewer.LineLeft();
                    }
                    else
                    {
                        scrollViewer.LineRight();
                    }

                    e.Handled = true;
                }
            }
        }

        #endregion Event Handlers

        #region SQL Event Handlers

        /// <summary>
        /// Handles the SQL button click event and populates the connection dropdown.
        /// </summary>
        private async void btnSql_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sqlServerConnections = SqlServerAgent.GetConnections();

                if (sqlServerConnections == null || sqlServerConnections.Count == 0)
                {
                    MessageBox.Show("No SQL Server connections were found. Please add connections first through the Server Explorer window.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                sqlServerConnections = sqlServerConnections.Where(c1 => !sqlServerConnectionsAlreadyAdded.Any(c2 => c2 == c1.ConnectionString)).ToList();

                if (sqlServerConnections == null || sqlServerConnections.Count == 0)
                {
                    MessageBox.Show("All available connections have been added to the context.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                cbConnection.ItemsSource = sqlServerConnections;

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
                apiChat.AppendFunctionCall(function);
                messagesForDatabase.Add(new() { Order = messagesForDatabase.Count + 1, Segments = [new() { Author = IdentifierEnum.FunctionCall, Content = JsonConvert.SerializeObject(function) }] });
            }

            string request = options.SqlServerAgentCommand + Environment.NewLine + dataBaseSchema + Environment.NewLine;

            SqlServerConnectionInfo connection = (SqlServerConnectionInfo)cbConnection.SelectedItem;

            string requestToShowOnList = TAG_SQL + connection.Description + Environment.NewLine + Environment.NewLine + options.SqlServerAgentCommand;

            messagesForDatabase.Add(new() { Order = messagesForDatabase.Count + 1, Segments = [new() { Author = IdentifierEnum.FunctionRequest, Content = request }] });

            await RequestAsync(RequestType.Request, request, requestToShowOnList, false);

            sqlServerConnectionsAlreadyAdded.Add(connection.ConnectionString);

            ChatRepository.AddSqlServerConnection(chatId, connection.ConnectionString);

            sqlServerConnections.Remove(connection);

            cbConnection.ItemsSource = sqlServerConnections;

            cbConnection.SelectedIndex = 0;

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

        #endregion SQL Event Handlers

        #region API Event Handlers

        /// <summary>
        /// Handles the click event for the API button. Retrieves API definitions, filters out already added definitions, 
        /// and updates the UI to display the remaining APIs. Displays appropriate messages if no APIs are found or all are already added.
        /// </summary>
        private async void btnAPI_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                apiDefinitions = ApiAgent.GetAPIsDefinitions();

                if (apiDefinitions == null || apiDefinitions.Count == 0)
                {
                    MessageBox.Show("No API definitions were found. Please add API definitions first through the extension's options window.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                apiDefinitions = apiDefinitions.Where(c1 => !apiDefinitionsAlreadyAdded.Any(c2 => c2 == c1.Name)).ToList();

                if (apiDefinitions == null || apiDefinitions.Count == 0)
                {
                    MessageBox.Show("All available APIs have been added to the context.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                cbAPIs.ItemsSource = apiDefinitions;

                cbAPIs.SelectedIndex = 0;

                grdCommands.Visibility = Visibility.Collapsed;
                grdAPI.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Handles the click event for the API send button. Sends API function calls, updates the chat and message list, 
        /// processes the selected API definition, and updates the UI accordingly.
        /// </summary>
        private async void btnApiSend_Click(object sender, RoutedEventArgs e)
        {
            List<FunctionRequest> apiFunctions = ApiAgent.GetApiFunctions();

            foreach (FunctionRequest function in apiFunctions)
            {
                apiChat.AppendFunctionCall(function);
                messagesForDatabase.Add(new() { Order = messagesForDatabase.Count + 1, Segments = [new() { Author = IdentifierEnum.FunctionCall, Content = JsonConvert.SerializeObject(function) }] });
            }

            ApiItem apiDefinition = (ApiItem)cbAPIs.SelectedItem;

            string request = string.Concat(options.APIAgentCommand, Environment.NewLine, "API Name: ", apiDefinition.Name, Environment.NewLine, TextFormat.MinifyText(apiDefinition.Definition, string.Empty));

            string requestToShowOnList = TAG_API + apiDefinition.Name + Environment.NewLine + Environment.NewLine + options.APIAgentCommand;

            messagesForDatabase.Add(new() { Order = messagesForDatabase.Count + 1, Segments = [new() { Author = IdentifierEnum.FunctionRequest, Content = request }] });

            await RequestAsync(RequestType.Request, request, requestToShowOnList, false);

            apiDefinitionsAlreadyAdded.Add(apiDefinition.Name);

            ChatRepository.AddApiDefinition(chatId, apiDefinition.Name);

            apiDefinitions.Remove(apiDefinition);

            cbAPIs.ItemsSource = apiDefinitions;

            cbAPIs.SelectedIndex = 0;

            grdAPI.Visibility = Visibility.Collapsed;
            grdCommands.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles the click event for the API cancel button. Cancels the current request and updates the UI by hiding the API grid and showing the commands grid.
        /// </summary>
        private async void btnApiCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelRequest(sender, e);

            grdAPI.Visibility = Visibility.Collapsed;
            grdCommands.Visibility = Visibility.Visible;
        }

        #endregion API Event Handlers        

        #region Methods  

        /// <summary>
        /// Handles an asynchronous request based on the specified command type. Validates input, appends context or system messages, processes code or image-related requests, and sends the final request.
        /// </summary>
        private async System.Threading.Tasks.Task RequestAsync(RequestType commandType)
        {
            shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (!IsReadyToSendRequest())
            {
                return;
            }

            if (!selectedContextFilesCodeAppended)
            {
                string selectedContextFilesCode = await GetSelectedContextItemsCodeAsync();

                if (!string.IsNullOrWhiteSpace(selectedContextFilesCode))
                {
                    apiChat.AppendSystemMessage(selectedContextFilesCode);
                    selectedContextFilesCodeAppended = true;
                }
            }

            if (commandType == RequestType.Code)
            {
                docView = await VS.Documents.GetActiveDocumentViewAsync();

                string originalCode = docView.TextView.TextBuffer.CurrentSnapshot.GetText();

                if (options.MinifyRequests)
                {
                    originalCode = TextFormat.MinifyText(originalCode, " ");
                }

                originalCode = TextFormat.RemoveCharactersFromText(originalCode, options.CharactersToRemoveFromRequests.Split(','));

                apiChat.AppendSystemMessage(options.TurboChatCodeCommand);
                apiChat.AppendUserInput(originalCode);
            }

            string requestToShowOnList = txtRequest.Text;

            if (attachedImage != null)
            {
                requestToShowOnList = TAG_IMG + txtImage.Text + Environment.NewLine + Environment.NewLine + requestToShowOnList;

                List<ChatContentForImage> chatContent = [new(attachedImage)];

                apiChat.AppendUserInput(chatContent);
            }

            string request = await completionManager.ReplaceReferencesAsync(txtRequest.Text);

            txtRequest.Text = string.Empty;

            await RequestAsync(commandType, request, requestToShowOnList, shiftKeyPressed);
        }

        /// <summary>
        /// Checks whether the necessary conditions are met to send a request, including authentication and request content.
        /// </summary>
        /// <returns>
        /// True if the request is ready to be sent; otherwise, false.
        /// </returns>
        private bool IsReadyToSendRequest()
        {
            if (!options.AzureEntraIdAuthentication && string.IsNullOrWhiteSpace(options.ApiKey))
            {
                MessageBox.Show(Constants.MESSAGE_SET_API_KEY, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                package.ShowOptionPage(typeof(OptionPageGridGeneral));

                return false;
            }

            if (string.IsNullOrWhiteSpace(txtRequest.Text))
            {
                MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends an asynchronous request based on the provided command type, processes the response, 
        /// and updates the UI elements accordingly. Handles exceptions and manages UI state during the operation.
        /// </summary>
        private async System.Threading.Tasks.Task RequestAsync(RequestType commandType, string request, string requestToShowOnList, bool shiftKeyPressed)
        {
            await ExecuteRequestWithCommonHandlingAsync(async () =>
            {
                AddMessagesHtml(IdentifierEnum.Me, requestToShowOnList);

                UpdateBrowser();

                request = options.MinifyRequests ? TextFormat.MinifyText(request, " ") : request;

                request = TextFormat.RemoveCharactersFromText(request, options.CharactersToRemoveFromRequests.Split(','));

                apiChat.AppendUserInput(request);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    EnableDisableButtons(false);
                }));

                messagesForDatabase.Add(new() { Order = messagesForDatabase.Count + 1, Segments = [new() { Author = IdentifierEnum.Me, Content = requestToShowOnList }] });

                cancellationTokenSource = new();

                (string, List<FunctionResult>) result = await SendRequestAsync(cancellationTokenSource);

                if (result.Item2 != null && result.Item2.Any())
                {
                    await HandleFunctionsCallsAsync(result.Item2, cancellationTokenSource);
                }
                else
                {
                    HandleResponse(commandType, shiftKeyPressed, result.Item1);
                }

                if (firstMessage)
                {
                    string request = "Please suggest a concise and relevant title for my first message based on its context, using up to three words and in the same language as my first message.";

                    await UpdateHeaderAsync(request, cancellationTokenSource);
                }
                else
                {
                    parentControl.NotifyNewChatMessagesAdded(ChatHeader, messagesForDatabase);
                }
            });
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
            Task<(string, List<FunctionResult>)> task = apiChat.GetResponseContentAndFunctionAsync();

            await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));

            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            return await task;
        }

        /// <summary>
        /// Enables or disables a set of buttons and updates the UI status message and visibility accordingly.
        /// </summary>
        /// <param name="enable">If true, buttons are enabled and visible; if false, buttons are disabled and some UI elements are shown.</param>
        /// <param name="computerCall">If true, indicates the action is triggered by the computer-use, updating the status message accordingly.</param>
        private void EnableDisableButtons(bool enable, bool computerCall = false)
        {
            if (computerCall)
            {
                lblProgressStatus.Text = "AI is executing actions. Please wait and avoid interaction until completion.";
            }
            else
            {
                lblProgressStatus.Text = "Waiting API Response.";
            }

            grdProgress.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;

            btnAPI.IsEnabled = enable;
            btnSql.IsEnabled = enable;
            btnAttachImage.IsEnabled = enable;
            btnComputerUse.IsEnabled = enable;
            btnRequestCode.IsEnabled = enable;
            btnRequestSend.IsEnabled = enable;
            btnSqlSend.IsEnabled = enable;
            btnCancel.IsEnabled = !enable;

            btnAPI.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnSql.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnAttachImage.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnComputerUse.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
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
        /// Updates the chat header based on the user's request by sending the request,
        /// processing the response to generate a chat name, and notifying the parent control of the new chat.
        /// </summary>
        /// <param name="request">The user's input request to be sent and processed.</param>
        /// <param name="cancellationToken">The cancellation token source to cancel the asynchronous operation if needed.</param>
        private async System.Threading.Tasks.Task UpdateHeaderAsync(string request, CancellationTokenSource cancellationToken)
        {
            apiChat.AppendUserInput(request);

            (string, List<FunctionResult>) result = await SendRequestAsync(cancellationToken);

            string chatName = result.Item1;

            chatName = TextFormat.RemoveCharactersFromText(chatName, "\r\n", "\n", "\r", ".", ",", ":", ";", "'", "\"");

            string[] words = chatName.Split(' ');

            if (words.Length > 3)
            {
                chatName = string.Concat(words[0], " ", words[1]);
            }

            ChatHeader.UpdateChatName(chatName);

            parentControl.NotifyNewChatCreated(ChatHeader, chatName, messagesForDatabase);

            firstMessage = false;
        }

        /// <summary>
        /// Handles the response based on the command type and shift key state, updating the document view or chat list control items accordingly.
        /// </summary>
        private void HandleResponse(RequestType commandType, bool shiftKeyPressed, string response)
        {
            if (commandType == RequestType.Code && !shiftKeyPressed)
            {
                List<ChatMessageSegment> segments = TextFormat.GetChatTurboResponseSegments(response);

                for (int i = 0; i < segments.Count; i++)
                {
                    if (segments[i].Author == IdentifierEnum.ChatGPTCode)
                    {
                        docView.TextView.TextBuffer.Replace(new Microsoft.VisualStudio.Text.Span(0, docView.TextView.TextBuffer.CurrentSnapshot.Length), segments[i].Content);
                    }
                    else
                    {
                        AddMessagesHtml(segments[i].Author, segments[i].Content);
                    }
                }
            }
            else
            {
                messagesForDatabase.Add(new() { Order = messagesForDatabase.Count + 1, Segments = [new() { Author = IdentifierEnum.ChatGPT, Content = response }] });
                AddMessagesHtml(IdentifierEnum.ChatGPT, response);
            }

            UpdateBrowser();
        }

        /// <summary>
        /// Handles a list of function calls asynchronously, processes their results, updates the UI, and recursively handles additional function calls if needed.
        /// </summary>
        /// <param name="functions">A list of functions to be executed and processed.</param>
        /// <param name="cancellationToken">A cancellation token to manage task cancellation.</param>
        /// <returns>
        /// A boolean indicating whether the response was successfully handled.
        /// </returns>
        private async System.Threading.Tasks.Task<bool> HandleFunctionsCallsAsync(List<FunctionResult> functions, CancellationTokenSource cancellationToken)
        {
            string functionResult;

            foreach (FunctionResult function in functions)
            {
                if (ApiAgent.GetApiFunctions().Select(f => f.Function.Name).Any(f => f.Equals(function.Function.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    (string, string) apiResponse = await ApiAgent.ExecuteFunctionAsync(function, options.LogAPIAgentRequestAndResponses);

                    functionResult = apiResponse.Item1;

                    if (!string.IsNullOrWhiteSpace(apiResponse.Item2))
                    {
                        messagesForDatabase.Add(new() { Order = messagesForDatabase.Count + 1, Segments = [new() { Author = IdentifierEnum.Api, Content = apiResponse.Item2 }] });
                        AddMessagesHtml(IdentifierEnum.Api, apiResponse.Item2);
                    }
                }
                else
                {
                    functionResult = SqlServerAgent.ExecuteFunction(function, options.LogSqlServerAgentQueries, out DataView readerResult);

                    if (readerResult != null && readerResult.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            dataGridResult.ItemsSource = null;
                            dataGridResult.ItemsSource = readerResult;
                            dataGridResult.Visibility = Visibility.Visible;
                        });
                    }
                }

                apiChat.AppendToolMessage(function.Id, functionResult);
            }

            (string, List<FunctionResult>) result = await SendRequestAsync(cancellationToken);

            bool responseHandled = false;

            if (result.Item2 != null && result.Item2.Any())
            {
                responseHandled = await HandleFunctionsCallsAsync(result.Item2, cancellationToken);
            }

            if (!responseHandled)
            {
                HandleResponse(RequestType.Request, false, result.Item1);

                responseHandled = true;
            }

            return responseHandled;
        }

        /// <summary>
        /// Recursively searches for a visual child of a specified type within a given DependencyObject.
        /// </summary>
        /// <typeparam name="T">The type of the visual child to find.</typeparam>
        /// <param name="obj">The parent DependencyObject to search within.</param>
        /// <returns>
        /// The first visual child of the specified type, or null if no such child is found.
        /// </returns>
        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child is T t)
                {
                    return t;
                }

                T childOfChild = FindVisualChild<T>(child);

                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }

        /// <summary>
        /// Closes the current tab by invoking the CloseTab method on the parent control.
        /// </summary>
        public void CloseTab(Object sender, ExecutedRoutedEventArgs e)
        {
            parentControl.CloseTab(this);
        }

        /// <summary>
        /// Adds a formatted HTML message to the message list, including the author's avatar and the response content converted from Markdown.
        /// </summary>
        /// <param name="author">The author of the message, used to determine the avatar image.</param>
        /// <param name="content">The message content in Markdown format to be converted and displayed.</param>
        private void AddMessagesHtml(IdentifierEnum author, string content)
        {
            string htmlContent;

            string authorIcon = author switch
            {
                IdentifierEnum.Me => meIcon,
                IdentifierEnum.ChatGPT => chatGptIcon,
                IdentifierEnum.Api => apiIcon
            };

            if (author == IdentifierEnum.Me)
            {
                content = HighlightSpecialTagsForHtml(content);

                content = content
                    .Replace(TAG_IMG, $"<img src='{imgIcon}' style='width:18px; height:18px; vertical-align:top; margin-right:3px;' />")
                    .Replace(TAG_SQL, $"<img src='{sqlIcon}' style='width:18px; height:18px; vertical-align:top; margin-right:3px;' />")
                    .Replace(TAG_API, $"<img src='{apiIcon}' style='width:18px; height:18px; vertical-align:top; margin-right:3px;' />");                

                htmlContent = content.Replace(Environment.NewLine, "<br />");
            }
            else
            {
                htmlContent = Markdown.ToHtml(content, markdownPipeline);
            }

            if (htmlContent.EndsWith("<br />"))
            {
                htmlContent = htmlContent.Substring(0, htmlContent.Length - 6);
            }

            htmlContent = htmlContent.Replace("<script", "&lt;script").Replace("</script>", "&lt;/script&gt;");

            string messageHtml = $@"
                    <div style='position: relative; margin-bottom: 16px; padding-top: 20px;'>
                        <img src='{authorIcon}' style='display: block; position: absolute; top: 0px; width: 40px; height: 40px;' />
                        <div style='margin-left: 0; margin-top: 20px; border: 1.5px solid #888; border-radius: 12px; padding: 5px 5px 5px 5px; box-sizing: border-box;'>
                            {htmlContent}
                        </div>
                    </div>";

            messagesHtml.AppendLine(messageHtml);
        }

        /// <summary>
        /// Updates the embedded web browser control with dynamically generated HTML content.
        /// </summary>
        private void UpdateBrowser()
        {
            Color textColor = ((SolidColorBrush)Application.Current.Resources[VsBrushes.WindowTextKey]).Color;
            Color backgroundColor = ((SolidColorBrush)Application.Current.Resources[VsBrushes.WindowKey]).Color;

            Color codeBackgroundColor = Color.FromRgb(
                (byte)Math.Max(0, backgroundColor.R - 10),
                (byte)Math.Max(0, backgroundColor.G - 10),
                (byte)Math.Max(0, backgroundColor.B - 10)
            );

            string cssTextColor = ToCssColor(textColor);
            string cssBackgroundColor = ToCssColor(backgroundColor);
            string cssCodeBackgroundColor = ToCssColor(codeBackgroundColor);

            string html = $@"
                    <html>
                    <head>
                        <meta http-equiv='X-UA-Compatible' content='IE=edge' />
                        <meta charset='UTF-8'>
                        <style>
                            body {{
                                font-family: Segoe UI, Tahoma, Geneva, Verdana, sans-serif;
                                margin: 10px;
                                background-color: {cssBackgroundColor};
                                color: {cssTextColor};
                            }}
                            p {{
                                margin-top: 1px;
                                margin-bottom: 1px;
                            }}
                            pre {{
                                overflow-x: auto;
                                white-space: pre;
                                background: {cssCodeBackgroundColor};
                                color: {cssTextColor};
                                padding: 8px;
                                font-family: Consolas, 'Courier New', monospace;
                                font-size: 13px;
                                margin: 6px 0;
                            }}
                            code {{
                                font-family: Consolas, 'Courier New', monospace;
                                font-size: 13px;
                                color: {cssTextColor};
                            }}
                            .copy-btn {{
                                position: absolute;
                                right: 8px;
                                top: 6px;
                                width: 15px;
                                height: 15px;
                                background: none;
                                border: none;
                                padding: 0;
                                cursor: pointer;
                                outline: none;
                            }}
                            .copy-btn img {{
                                width: 15px;
                                height: 15px;
                                display: block;
                            }}
                        </style>
                        <script type='text/javascript'>
                            var copyIcon = '{copyIcon}';
                            var checkIcon = '{checkIcon}';

                            window.onload = function() {{
                                window.scrollTo(0, document.body.scrollHeight);
                                
                                document.body.focus();

                                document.body.addEventListener('keydown', function(e) {{
                                    var amount = 40;
                                    switch (e.key) {{
                                        case 'ArrowDown': window.scrollBy(0, amount); e.preventDefault(); break;
                                        case 'ArrowUp': window.scrollBy(0, -amount); e.preventDefault(); break;
                                        case 'PageDown': window.scrollBy(0, window.innerHeight - 40); e.preventDefault(); break;
                                        case 'PageUp': window.scrollBy(0, -window.innerHeight + 40); e.preventDefault(); break;
                                        case 'Home': window.scrollTo(0, 0); e.preventDefault(); break;
                                        case 'End': window.scrollTo(0, document.body.scrollHeight); e.preventDefault(); break;
                                    }}
                                }});     

                                var pres = document.getElementsByTagName('pre');

                                for (var i = 0; i < pres.length; i++) {{
                                    var pre = pres[i];
                                    
                                    var btn = document.createElement('button');
                                    btn.className = 'copy-btn';
                                    btn.title = 'Copy';

                                    var img = document.createElement('img');
                                    img.src = copyIcon;
                                    btn.appendChild(img);

                                    var wrapper = document.createElement('div');
                                    wrapper.style.position = 'relative';

                                    pre.parentNode.insertBefore(wrapper, pre);
                                    wrapper.appendChild(pre);

                                    wrapper.appendChild(btn);

                                    btn.onclick = function() {{
                                        var code = this.parentNode.getElementsByTagName('pre')[0].innerText;
                                        
                                        if (window.clipboardData && window.clipboardData.setData) {{
                                            window.clipboardData.setData('Text', code);
                                        }} else if (navigator.clipboard) {{
                                            navigator.clipboard.writeText(code);
                                        }} else {{
                                            var textarea = document.createElement('textarea');
                                            textarea.value = code;
                                            document.body.appendChild(textarea);
                                            textarea.select();
                                            try {{ document.execCommand('copy'); }} catch(e) {{}}
                                            document.body.removeChild(textarea);
                                        }}
                                        var img = this.getElementsByTagName('img')[0];
                                        img.src = checkIcon;
                                        var btn = this;
                                        setTimeout(function() {{ img.src = copyIcon; }}, 1200);
                                    }};

                                    pre.addEventListener('wheel', function(e) {{
                                        if (e.shiftKey) {{
                                            this.scrollLeft += (e.deltaY || e.detail || e.wheelDelta) > 0 ? 40 : -40;
                                            e.preventDefault();
                                        }}
                                    }});
                                }}
                            }};
                        </script>
                    </head>
                    <body tabindex=""0"">
                        {messagesHtml}
                    </body>
                    </html>";

            webBrowserChat.NavigateToString(html);
        }

        /// <summary>
        /// Retrieves the Base64-encoded string representation of an image associated with the specified <paramref name="identifier"/>.
        /// </summary>
        /// <param name="identifier">The identifier enum value representing the image to retrieve.</param>
        /// <returns>
        /// A string containing the Base64-encoded image data prefixed with the appropriate data URI scheme.
        /// </returns>
        private string GetImageBase64(IdentifierEnum identifier)
        {
            if (base64Images.TryGetValue(identifier, out string result))
            {
                return result;
            }

            string imageSource = identifier switch
            {
                IdentifierEnum.Me => "pack://application:,,,/VisualChatGPTStudio;component/Resources/vs.png",
                IdentifierEnum.ChatGPT => "pack://application:,,,/VisualChatGPTStudio;component/Resources/chatGPT.png",
                IdentifierEnum.Api => "pack://application:,,,/VisualChatGPTStudio;component/Resources/api.png",
                IdentifierEnum.CopyIcon => "pack://application:,,,/VisualChatGPTStudio;component/Resources/copy.png",
                IdentifierEnum.CheckIcon => "pack://application:,,,/VisualChatGPTStudio;component/Resources/check.png",
                IdentifierEnum.SqlIcon => "pack://application:,,,/VisualChatGPTStudio;component/Resources/db.png",
                IdentifierEnum.ImgIcon => "pack://application:,,,/VisualChatGPTStudio;component/Resources/image.png"
            };

            Uri uri = new(imageSource, UriKind.RelativeOrAbsolute);

            System.Windows.Resources.StreamResourceInfo streamResourceInfo = Application.GetResourceStream(uri);

            string image;

            using (System.IO.Stream stream = streamResourceInfo.Stream)
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                string base64 = Convert.ToBase64String(buffer);
                image = $"data:image/png;base64,{base64}";
            }

            base64Images.Add(identifier, image);

            return image;
        }

        /// <summary>
        /// Highlights special tags in the input string for HTML.
        /// </summary>
        /// <param name="input">The input string containing text with special tags to highlight.</param>
        /// <returns>
        /// A string where words starting with '/' are wrapped in a green bold span, and words starting with '@' 
        /// are wrapped in a purple bold span, suitable for HTML rendering.
        /// </returns>
        private string HighlightSpecialTagsForHtml(string input)
        {
            // Regex: Find words that start with / or @ and end with a space, comma, newline, or end of string.
            return Regex.Replace(
                input,
                @"(?<=^|[\s,])([/@][^\s,\r\n]*)",
                match =>
                {
                    string word = match.Value;
                    if (word.StartsWith("/"))
                    {
                        return $"<span style='color: #2ecc40; font-weight: bold;'>{word}</span>";
                    }

                    if (word.StartsWith("@"))
                    {
                        return $"<span style='color: #8e44ad; font-weight: bold;'>{word}</span>";
                    }

                    return word;
                });
        }

        /// <summary>
        /// Executes the specified asynchronous operation with common exception handling for requests.
        /// </summary>
        /// <param name="requestOperation">The asynchronous operation to execute.</param>
        private async System.Threading.Tasks.Task ExecuteRequestWithCommonHandlingAsync(Func<System.Threading.Tasks.Task> requestOperation)
        {
            try
            {
                await requestOperation();
            }
            catch (OperationCanceledException)
            {
                // catch request cancelation
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                EnableDisableButtons(true);
                spImage.Visibility = Visibility.Collapsed;
                attachedImage = null;
            }
        }

        /// <summary>
        /// Converts a <see cref="System.Windows.Media.Color"/> to its CSS hexadecimal color string representation (e.g., "#RRGGBB").
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>
        /// A string representing the color in CSS hexadecimal format.
        /// </returns>
        private static string ToCssColor(System.Windows.Media.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Processes the computer use response asynchronously by displaying messages, executing computer actions,
        /// capturing screenshots, and sending subsequent requests until no further actions are available.
        /// </summary>
        /// <param name="response">The initial ComputerUseResponse containing output items to process.</param>
        private async System.Threading.Tasks.Task ProcessComputerUseResponseAsync(ComputerUseResponse response)
        {
            while (true)
            {
                previousResponseId = response.Id;

                // 1. Display messages and reasoning in the UI
                List<ComputerUseContent> messages = response.Output
                    .Where(o => o.Type == ComputerUseOutputItemType.reasoning && o.Summary != null)
                    .SelectMany(o => o.Summary).ToList();

                messages.AddRange(response.Output
                    .Where(o => o.Type == ComputerUseOutputItemType.message && o.Content != null)
                    .SelectMany(o => o.Content));

                foreach (ComputerUseContent message in messages)
                {
                    AddMessagesHtml(IdentifierEnum.ChatGPT, message.Text);

                    messagesForDatabase.Add(new()
                    {
                        Order = messagesForDatabase.Count + 1,
                        Segments = [new() { Author = IdentifierEnum.ChatGPT, Content = message.Text }]
                    });
                }

                UpdateBrowser();

                // 2. Find the next computer_call action
                ComputerUseOutputItem computerCall = response.Output.FirstOrDefault(o => o.Type == ComputerUseOutputItemType.computer_call);

                if (computerCall == null)
                {
                    // No more actions to perform
                    if (firstMessage)
                    {
                        string request = $"Please suggest a concise and relevant title for the message \"{messagesForDatabase[0].Segments[0].Content}\", based on its context, using up to three words and in the same language as the message.";

                        await UpdateHeaderAsync(request, cancellationTokenSource);
                    }
                    else
                    {
                        parentControl.NotifyNewChatMessagesAdded(ChatHeader, messagesForDatabase);
                    }

                    break;
                }

                // 3. Execute the action programmatically in Windows/Visual Studio
                ComputerUse.DoActionAsync(computerCall.Action, screenBounds);

                // 4. Capture the updated screenshot
                byte[] screenshot = ScreenCapturer.CaptureFocusedScreenScreenshot(out screenBounds);

                // 5. Send the next request using the output of the action
                response = await ApiHandler.GetComputerUseResponseAsync(
                    options,
                    screenBounds.Width,
                    screenBounds.Height,
                    screenshot,
                    computerCall.CallId,
                    response.Id,
                    computerCall.PendingSafetyChecks,
                    cancellationTokenSource.Token
                );
            }
        }

        #endregion Methods   
    }
}