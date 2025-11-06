using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Agents;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using JeffPires.VisualChatGPTStudio.Utils.API;
using JeffPires.VisualChatGPTStudio.Utils.CodeCompletion;
using JeffPires.VisualChatGPTStudio.Utils.Repositories;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using OpenAI_API.Chat;
using OpenAI_API.Functions;
using OpenAI_API.ResponsesAPI.Models.Request;
using OpenAI_API.ResponsesAPI.Models.Response;
using VisualChatGPTStudioShared.Agents.ApiAgent;
using VisualChatGPTStudioShared.ToolWindows.Turbo;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using JsonElement = System.Text.Json.JsonElement;
using MessageBox = System.Windows.MessageBox;
using Task = System.Threading.Tasks.Task;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    /// <summary>
    /// Interaction logic for TerminalWindowTurboControl.
    /// </summary>
    public partial class TerminalWindowTurboControl
    {
        #region Constants

        private const string TAG_IMG = "#IMG#";
        private const string TAG_SQL = "#SQL#";
        private const string TAG_API = "#API#";

        #endregion Constants

        #region Properties

        private Package package;
        private bool webView2Installed;
        private readonly TerminalTurboViewModel _viewModel;
        private IWebView2 _webView;

        private CancellationTokenSource cancellationTokenSource;
        private DocumentView docView;
        private bool shiftKeyPressed;
        private bool selectedContextFilesCodeAppended;
        private CompletionManager completionManager;
        private Rectangle screenBounds;
        private string previousResponseId;
        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowTurboControl"/> class.
        /// </summary>
        public TerminalWindowTurboControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;

            _viewModel = (TerminalTurboViewModel)DataContext;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Starts the control with the given options and package.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="package">The package.</param>
        public async void StartControl(OptionPageGridGeneral options, Package package)
        {
            _viewModel.options = options;
            this.package = package;

            if (!webView2Installed)
            {
                webView2Installed = await WebView2BootstrapperHelper.EnsureRuntimeAvailableAsync();

                if (!webView2Installed)
                {
                    return;
                }
            }

            txtRequest.MaxHeight = rowRequest.MaxHeight - 10;
            txtRequest.TextArea.TextEntering += txtRequest_TextEntering;
            txtRequest.TextArea.TextEntered += txtRequest_TextEntered;
            txtRequest.PreviewKeyDown += OnTxtRequestOnPreviewKeyDown;

            _viewModel.Messages.CollectionChanged += MessagesOnCollectionChanged;

            AttachImage.OnImagePaste += AttachImage_OnImagePaste;

            completionManager = new CompletionManager(package, txtRequest);

            VSColorTheme.ThemeChanged += _ =>
            {
                try
                {
                    WebAsset.DeployTheme();
                    _webView?.ExecuteScriptAsync(WebFunctions.ReloadThemeCss(WebAsset.IsDarkTheme));
                }
                catch (Exception e)
                {
                    Logger.Log(e);
                }
            };
        }

        private async void MessagesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null || e.NewItems.Count == 0)
            {
                _webView?.ExecuteScriptAsync(WebFunctions.ClearChat);
            }
            else
            {
                try
                {
                    foreach (MessageEntity message in e.NewItems)
                    {
                        if (message.Segments == null || message.Segments.Count == 0)
                        {
                            continue;
                        }
                        var author = message.Segments[0].Author;
                        var content = string.Join("", message.Segments.Select(s => s.Content));
                        if (author == IdentifierEnum.FunctionCall)
                        {
                            if (!_viewModel.options.ShowToolCalls)
                            {
                                continue;
                            }

                            try
                            {
                                var function = JsonSerializer.Deserialize<FunctionToCall>(content, _serializeOptions);
                                content = $"""
                                           <details><summary>Tool: {function.Name}</summary>

                                           ```Arguments
                                           {function.Arguments}
                                           ```

                                           ```Result
                                           {function.Result}
                                           ```

                                           </details>
                                           """;
                            }
                            catch (Exception exception)
                            {
                                content += $"- Failed to deserialize: `{exception.Message}`";
                            }
                        }

                        if (string.IsNullOrEmpty(content))
                        {
                            continue;
                        }

                        var script = author == IdentifierEnum.ChatGPT
                            ? WebFunctions.UpdateLastGpt(content)
                            : WebFunctions.AddMsg(author, content);

                        await _webView!.ExecuteScriptAsync(script);

                        if (author == IdentifierEnum.ChatGPT)
                        {
                            _webView?.ExecuteScriptAsync(WebFunctions.RenderMermaid);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logger.Log(exception);
                    MessageBox.Show(exception.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    _ = _webView.CoreWebView2.BrowserProcessId;
                    return;
                }
            }
            catch (Exception ex)
            {
                WebViewHost.Content = null;
                _webView?.Dispose();
                _webView = null;
            }

            if (!webView2Installed)
            {
                webView2Installed = await WebView2BootstrapperHelper.EnsureRuntimeAvailableAsync();

                if (!webView2Installed)
                {
                    return;
                }
            }

#if COPILOT_ENABLED //VS2022
            _webView = new WebView2CompositionControl();
#else //VS2019
            _webView = new WebView2();
#endif
            _webView.CoreWebView2InitializationCompleted += CoreWebView2InitializationCompleted;
            _webView.NavigationCompleted += (o, args) =>
            {
                _webView?.ExecuteScriptAsync(WebFunctions.ReloadThemeCss(WebAsset.IsDarkTheme));
                _viewModel.ForceDownloadChats();
                _viewModel.LoadChat();
            };

            WebViewHost.Content = _webView;

            var env = await CoreWebView2Environment.CreateAsync(null,
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            await _webView.EnsureCoreWebView2Async(env);

            _webView.WebMessageReceived += WebViewOnWebMessageReceived;
        }

        private async void WebViewOnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs webMessage)
        {
            if (webMessage?.WebMessageAsJson == null)
                return;

            try
            {
                var msg = JsonSerializer.Deserialize<JsonElement>(webMessage.WebMessageAsJson);
                var action = msg.GetProperty("action").GetString()?.ToLower();
                var data = msg.GetProperty("data").GetString();
                if (data == null)
                    return;

                switch (action)
                {
                    case "png":
                        var pngBytes = Convert.FromBase64String(data);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(pngBytes);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        Clipboard.SetImage(bitmap);
                        break;
                    case "copy":
                        Clipboard.SetText(data);
                        break;
                    case "apply":
                        await TerminalWindowHelper.ApplyCodeToActiveDocumentAsync(data);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"WebView error: {e.Message}");

            }
        }

        private void CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Logger.Log($"WebView error: {e.InitializationException}");
                return;
            }
            _webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
#if !DEBUG
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#else
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#endif
            _webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            _webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
            _webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
            _webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = true;
            _webView.CoreWebView2.Settings.IsScriptEnabled = true;
            _webView.CoreWebView2.Settings.IsZoomControlEnabled = true;
            _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            _webView.DefaultBackgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundBrushKey);
            _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            _webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            try
            {
                _webView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
            }
            catch (Exception ex)
            {
                Logger.Log($"WebView MemoryUsageTargetLevel error: {ex}");
            }

            UpdateBrowser();
        }

        private void OnTxtRequestOnPreviewKeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !completionManager.IsShowed)
            {
                if (_viewModel.options.UseEnter)
                {
                    switch (Keyboard.Modifiers)
                    {
                        case ModifierKeys.Control:
                            // add new line
                            txtRequest.AppendText(Environment.NewLine);
                            e.Handled = true;
                            break;
                        case ModifierKeys.None:
                            // send Request by Enter
                            e.Handled = true;
                            _ = RequestAsync(RequestType.Request);
                            break;
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // send Request by Ctrl+Enter
                    e.Handled = true;
                    _ = RequestAsync(RequestType.Request);
                }
            }
            else
            {
                AttachImage.TextEditor_PreviewKeyDown(s, e);
            }
        }

        /// <summary>
        /// Handles an asynchronous request based on the specified command type. Validates input, appends context or system messages, processes code or image-related requests, and sends the final request.
        /// </summary>
        private async Task RequestAsync(RequestType commandType)
        {
            shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (!IsReadyToSendRequest())
            {
                return;
            }

            if (!selectedContextFilesCodeAppended)
            {
                var selectedContextFilesCode = await GetSelectedContextItemsCodeAsync();

                if (!string.IsNullOrWhiteSpace(selectedContextFilesCode))
                {
                    _viewModel.apiChat.AppendSystemMessage(selectedContextFilesCode);
                    selectedContextFilesCodeAppended = true;
                }
            }

            if (commandType == RequestType.Code)
            {
                docView = await VS.Documents.GetActiveDocumentViewAsync();

                var originalCode = docView?.TextView?.TextBuffer?.CurrentSnapshot.GetText();

                if (originalCode == null)
                {
                    return;
                }

                if (_viewModel.options.MinifyRequests)
                {
                    originalCode = TextFormat.MinifyText(originalCode, " ");
                }

                originalCode = TextFormat.RemoveCharactersFromText(originalCode, _viewModel.options.CharactersToRemoveFromRequests.Split(','));

                _viewModel.apiChat.AppendSystemMessage(_viewModel.options.TurboChatCodeCommand);
                _viewModel.apiChat.AppendUserInput(originalCode);
            }

            var requestToShowOnList = txtRequest.Text;

            if (_viewModel.AttachedImage != null)
            {
                requestToShowOnList = TAG_IMG + txtImage.Text + Environment.NewLine + Environment.NewLine + requestToShowOnList;

                List<ChatContentForImage> chatContent = [new(_viewModel.AttachedImage)];

                _viewModel.apiChat.AppendUserInput(chatContent);
            }

            var request = await completionManager.ReplaceReferencesAsync(txtRequest.Text);

            txtRequest.Text = string.Empty;

            await RequestAsync(commandType, request, requestToShowOnList, shiftKeyPressed);
        }

        private void ToggleTool(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton { IsChecked: true } toggleButton)
            {
                return;
            }

            if (toggleButton == ToggleSql)
            {
                _viewModel.SqlServerConnections = SqlServerAgent.GetConnections();

                if (_viewModel.SqlServerConnections.Count != 0)
                {
                    cbConnection.SelectedIndex = 0;
                    ToggleApi.IsChecked = false;
                }
                else
                {
                    cbConnection.SelectedIndex = -1;
                    toggleButton.IsChecked = false;
                    MessageBox.Show("No SQL Server connections were found. Please add connections first through the Server Explorer window.", Constants.EXTENSION_NAME,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else if (toggleButton == ToggleApi)
            {
                _viewModel.ApiDefinitions = ApiAgent.GetAPIsDefinitions();

                if (_viewModel.ApiDefinitions.Count != 0)
                {
                    cbAPIs.SelectedIndex = 0;
                    ToggleSql.IsChecked = false;
                }
                else
                {
                    cbAPIs.SelectedIndex = -1;
                    toggleButton.IsChecked = false;
                    MessageBox.Show("No API definitions were found. Please add API definitions first through the extension's options window.", Constants.EXTENSION_NAME,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private string UpdateTools(string input)
        {
            var result = string.Empty;
            _viewModel.apiChat.ClearTools();
            if (ToggleSql.IsChecked == true && cbConnection.SelectedItem is SqlServerConnectionInfo sqlServerConnectionInfo)
            {
                var dataBaseSchema = SqlServerAgent.GetDataBaseSchema(sqlServerConnectionInfo.ConnectionString);
                var sqlFunctions = SqlServerAgent.GetSqlFunctions();
                foreach (var function in sqlFunctions)
                {
                    _viewModel.apiChat.AppendFunctionCall(function);
                }

                result = $"""
                          {_viewModel.options.SqlServerAgentCommand}
                          <dataBaseSchema>{dataBaseSchema}</dataBaseSchema>
                          """;
            }
            if (ToggleApi.IsChecked == true && cbAPIs.SelectedItem is ApiItem apiDefinition)
            {
                var apiFunctions = ApiAgent.GetApiFunctions();
                foreach (var function in apiFunctions)
                {
                    _viewModel.apiChat.AppendFunctionCall(function);
                }

                result = $"""
                          {_viewModel.options.APIAgentCommand}
                          <apiName>{apiDefinition.Name}</apiName>
                          <apiDefinition>{TextFormat.MinifyText(apiDefinition.Definition, string.Empty)}</apiDefinition>
                          <task>{input}</task>
                          """;
            }

            return result;
        }

        /// <summary>
        /// Checks whether the necessary conditions are met to send a request, including authentication and request content.
        /// </summary>
        /// <returns>
        /// True if the request is ready to be sent; otherwise, false.
        /// </returns>
        private bool IsReadyToSendRequest()
        {
            if (!_viewModel.options.AzureEntraIdAuthentication && string.IsNullOrWhiteSpace(_viewModel.options.ApiKey))
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
        private async Task RequestAsync(RequestType commandType, string request, string requestToShowOnList, bool shiftKeyPressed)
        {
            var firstMessage = !_viewModel.Messages.Any();
            _viewModel.apiChat.UpdateApi(_viewModel.options.ApiKey, _viewModel.options.BaseAPI);
            if (!string.IsNullOrEmpty(_viewModel.options.Model))
            {
                _viewModel.apiChat.RequestParameters.Model = _viewModel.options.Model;
            }

            _viewModel.ToolCallAttempt = 0;

            await ExecuteRequestWithCommonHandlingAsync(async () =>
            {
                _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.Me, Content = requestToShowOnList });

                request = _viewModel.options.MinifyRequests ? TextFormat.MinifyText(request, " ") : request;
                request = TextFormat.RemoveCharactersFromText(request, _viewModel.options.CharactersToRemoveFromRequests.Split(','));
                request = UpdateTools(request);

                _viewModel.apiChat.AppendUserInput(request);
                Application.Current.Dispatcher.Invoke(() => { EnableDisableButtons(false); });

                cancellationTokenSource = new();

                if (_viewModel.options.CompletionStream)
                {
                    var segment = new ChatMessageSegment { Author = IdentifierEnum.ChatGPT };
                    _viewModel.AddMessageSegment(segment);
                    var content = new StringBuilder();
                    var chatResponses = _viewModel.apiChat.StreamResponseEnumerableFromChatbotAsync(cancellationTokenSource.Token);
                    await foreach (var fragment in chatResponses)
                    {
                        content.Append(fragment);
                        // TODO: send fragment instead full content
                        if (content.Length > 0) // dont show empty bubble
                        {
                            await _webView?.ExecuteScriptAsync(WebFunctions.UpdateLastGpt(content.ToString()))!;
                        }
                    }
                    segment.Content = content.ToString();

                    await HandleFunctionsCallsAsync(_viewModel.apiChat.StreamFunctionResults, cancellationTokenSource);
                }
                else
                {
                    (string, List<FunctionResult>) result = await SendRequestAsync(cancellationTokenSource);

                    if (result.Item2 != null && result.Item2.Any())
                    {
                        await HandleFunctionsCallsAsync(result.Item2, cancellationTokenSource);
                    }
                    else
                    {
                        HandleResponse(commandType, shiftKeyPressed, result.Item1);
                    }
                }

                // restore request in message history
                _viewModel.apiChat.ReplaceLastUserInput(requestToShowOnList);
                await _webView?.ExecuteScriptAsync(WebFunctions.RenderMermaid)!;

                if (firstMessage)
                {
                    var request =
                        "Please suggest a concise and relevant title for my first message based on its context, using up to three words and in the same language as my first message.";

                    await UpdateHeaderAsync(request, cancellationTokenSource);
                }

                ChatRepository.UpdateMessages(_viewModel.ChatId, _viewModel.Messages.ToList());
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
            Task<(string, List<FunctionResult>)> task = _viewModel.apiChat.GetResponseContentAndFunctionAsync();

            await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));

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

            btnAttachImage.IsEnabled = enable;
            btnComputerUse.IsEnabled = enable;
            btnRequestCode.IsEnabled = enable;
            btnRequestSend.IsEnabled = enable;
            btnCancel.IsEnabled = !enable;

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
        private async Task UpdateHeaderAsync(string request, CancellationTokenSource cancellationToken)
        {
            _viewModel.apiChat.MessagesCheckpoint();
            _viewModel.apiChat.AppendUserInput(request);

            (string, List<FunctionResult>) result = await SendRequestAsync(cancellationToken);

            string chatName = Regex.Replace(result.Item1, @"^<think>.*<\/think>", "", RegexOptions.Singleline);

            chatName = TextFormat.RemoveCharactersFromText(chatName, "\r\n", "\n", "\r", ".", ",", ":", ";", "'", "\"").Trim('*');

            string[] words = chatName.Split(' ');

            if (words.Length > 5)
            {
                chatName = string.Join(" ", words[0], words[1], words[2], words[3]);
            }

            _viewModel.apiChat.MessagesRestoreFromCheckpoint();
            _viewModel.UpdateChatHeader(chatName);
        }

        /// <summary>
        /// Handles the response based on the command type and shift key state, updating the document view or chat list control items accordingly.
        /// </summary>
        private void HandleResponse(RequestType commandType, bool shiftKeyPressed, string response)
        {
            if (commandType == RequestType.Code && !shiftKeyPressed)
            {
                var segments = TextFormat.GetChatTurboResponseSegments(response);
                var stringBuilder = new StringBuilder();

                foreach (var t in segments)
                {
                    if (t.Author == IdentifierEnum.ChatGPTCode && docView?.TextView != null)
                    {
                        docView.TextView.TextBuffer.Replace(new Span(0, docView.TextView.TextBuffer.CurrentSnapshot.Length), t.Content);
                    }
                    stringBuilder.AppendLine(t.Content);
                }

                _viewModel.AddMessageSegment(new ChatMessageSegment { Author = segments[0].Author, Content = stringBuilder.ToString() });
            }
            else
            {
                _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.ChatGPT, Content = response });
            }
        }

        private JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = true,
            MaxDepth = 10,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Handles a list of function calls asynchronously, processes their results, updates the UI, and recursively handles additional function calls if needed.
        /// </summary>
        /// <param name="functions">A list of functions to be executed and processed.</param>
        /// <param name="cancellationToken">A cancellation token to manage task cancellation.</param>
        /// <returns>
        /// A boolean indicating whether the response was successfully handled.
        /// </returns>
        private async Task<bool> HandleFunctionsCallsAsync(List<FunctionResult> functions, CancellationTokenSource cancellationToken)
        {
            string functionResult;

            if (functions.Count == 0)
            {
                return false;
            }

            foreach (FunctionResult function in functions)
            {
                // TODO: Approve before to calling (auto or manual)
                if (ApiAgent.GetApiFunctions().Select(f => f.Function.Name).Any(f => f.Equals(function.Function.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    (string FunctionResult, string Content) apiResponse = await ApiAgent.ExecuteFunctionAsync(function, _viewModel.options.LogAPIAgentRequestAndResponses);

                    functionResult = apiResponse.FunctionResult;

                    if (!string.IsNullOrWhiteSpace(apiResponse.Content))
                    {
                        functionResult = apiResponse.Content;
                    }
                }
                else
                {
                    functionResult = SqlServerAgent.ExecuteFunction(function, _viewModel.options.LogSqlServerAgentQueries, out DataView readerResult);
                    if (readerResult is { Count: > 0 })
                    {
                        var dataTable = readerResult.ToTable();
                        var selectResult = dataTable.ToMarkdown();

                        // Showing the selected result in chat without sending it to LLM
                        _viewModel.AddMessageSegment(new ChatMessageSegment { Author = IdentifierEnum.ChatGPT, Content = selectResult});
                    }
                }

                _viewModel.ToolCallAttempt++;

                _viewModel.apiChat.AppendToolMessage(function, functionResult);
                function.Function.Result = functionResult;
                _viewModel.AddMessageSegment(new ChatMessageSegment { Author = IdentifierEnum.FunctionCall, Content = JsonSerializer.Serialize(function.Function, _serializeOptions)});
            }

            if (_viewModel.ToolCallAttempt >= _viewModel.ToolCallMaxAttempts)
            {
                // Preventing an endless loop of calling tools
                // No tools - no calls ¯\_(ツ)_/¯
                _viewModel.apiChat.ClearTools();
            }

            (string Content, List<FunctionResult> ListFunctions) result = await SendRequestAsync(cancellationToken);


            var responseHandled = false;
            if (result.ListFunctions != null && result.ListFunctions.Any())
            {
                responseHandled = await HandleFunctionsCallsAsync(result.ListFunctions, cancellationToken);
            }

            if (!responseHandled)
            {
                HandleResponse(RequestType.Request, false, result.Content);
            }

            return true;
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
        /// Adds a formatted HTML message to the message list, including the author's avatar and the response content converted from Markdown.
        /// </summary>
        /// <param name="author">The author of the message, used to determine the avatar image.</param>
        /// <param name="content">The message content in Markdown format to be converted and displayed.</param>
        private async Task AddMessagesHtmlAsync(IdentifierEnum author, string content)
        {

        }

        /// <summary>
        /// Updates the embedded web browser control with dynamically generated HTML content.
        /// </summary>
        private void UpdateBrowser()
        {
            if (!webView2Installed)
            {
                return;
            }

            WebAsset.DeployTheme();
            _webView?.CoreWebView2.Navigate(WebAsset.GetTurboPath());
        }

        /// <summary>
        /// Executes the specified asynchronous operation with common exception handling for requests.
        /// </summary>
        /// <param name="requestOperation">The asynchronous operation to execute.</param>
        private async Task ExecuteRequestWithCommonHandlingAsync(Func<Task> requestOperation)
        {
            try
            {
                await requestOperation();
            }
            catch (OperationCanceledException)
            {
                // catch request cancellation
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
                _viewModel.AttachedImage = null;
            }
        }

        /// <summary>
        /// Processes the computer use response asynchronously by displaying messages, executing computer actions,
        /// capturing screenshots, and sending subsequent requests until no further actions are available.
        /// </summary>
        /// <param name="response">The initial ComputerUseResponse containing output items to process.</param>
        private async Task ProcessComputerUseResponseAsync(ComputerUseResponse response, bool firstMessage)
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
                    _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.ChatGPT, Content = message.Text });
                }

                // 2. Find the next computer_call action
                ComputerUseOutputItem computerCall = response.Output.FirstOrDefault(o => o.Type == ComputerUseOutputItemType.computer_call);

                if (computerCall == null)
                {
                    // No more actions to perform
                    if (firstMessage)
                    {
                        var request =
                            $"Please suggest a concise and relevant title for the message" +
                            $" \"{_viewModel.Messages[0].Segments[0].Content}\"," +
                            $" based on its context, using up to four-five words and in the same language as the message.";

                        await UpdateHeaderAsync(request, cancellationTokenSource);
                    }
                    break;
                }

                // 3. Execute the action programmatically in Windows/Visual Studio
                await ComputerUse.DoActionAsync(computerCall.Action, screenBounds);

                // 4. Capture the updated screenshot
                byte[] screenshot = ScreenCapturer.CaptureFocusedScreenScreenshot(out screenBounds);

                // 5. Send the next request using the output of the action
                response = await ApiHandler.GetComputerUseResponseAsync(
                    _viewModel.options,
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
            var firstMessage = !_viewModel.Messages.Any();
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

                _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.Me, Content = request });

                cancellationTokenSource = new();

                Task<ComputerUseResponse> task;

                if (!string.IsNullOrWhiteSpace(previousResponseId))
                {
                    task = ApiHandler.GetComputerUseResponseAsync(_viewModel.options, request, screenBounds.Width, screenBounds.Height, previousResponseId, cancellationTokenSource.Token);
                }
                else
                {
                    task = ApiHandler.GetComputerUseResponseAsync(_viewModel.options, request, screenBounds.Width, screenBounds.Height, screenshot, cancellationTokenSource.Token);
                }

                await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));

                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                ComputerUseResponse response = await task;

                await ProcessComputerUseResponseAsync(response, firstMessage);
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
        public void CancelRequest(object sender, RoutedEventArgs e)
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
            if (AttachImage.ShowDialog(out _viewModel.AttachedImage, out string imageName))
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
            _viewModel.AttachedImage = null;
        }

        /// <summary>
        /// Handles the event when an image is pasted, attaching the image and updating the UI with the file name.
        /// </summary>
        /// <param name="attachedImage">The byte array representing the pasted image.</param>
        /// <param name="fileName">The name of the pasted image file.</param>
        private void AttachImage_OnImagePaste(byte[] attachedImage, string fileName)
        {
            _viewModel.AttachedImage = attachedImage;
            txtImage.Text = fileName;
            spImage.Visibility = Visibility.Visible;
        }

        #endregion Event Handlers
    }
}

