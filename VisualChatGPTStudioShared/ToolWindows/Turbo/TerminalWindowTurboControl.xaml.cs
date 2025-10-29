using System;
using System.Collections.Generic;
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
using System.Windows.Controls;
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

        private OptionPageGridGeneral options;
        private Package package;
        private bool webView2Installed;
        private TerminalTurboViewModel _viewModel;
        private IWebView2? _webView;

        private Conversation apiChat;
        private CancellationTokenSource cancellationTokenSource;
        private DocumentView docView;
        private bool shiftKeyPressed;
        private bool selectedContextFilesCodeAppended;
        private CompletionManager completionManager;
        private byte[] attachedImage;
        private List<SqlServerConnectionInfo> sqlServerConnections;
        private List<ApiItem> apiDefinitions;
        private Rectangle screenBounds;
        private string previousResponseId;
        private string apiIcon;
        private string copyIcon;
        private string checkIcon;
        private string sqlIcon;
        private string imgIcon;

        private bool _isConfigOpen;
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
            this.options = options;
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
                _viewModel.LoadChat();
                _ = AddMessagesFromModelAsync();
            };

            WebViewHost.Content = _webView;

            var env = await CoreWebView2Environment.CreateAsync(null,
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            await _webView.EnsureCoreWebView2Async(env);

            apiChat = ApiHandler.CreateConversation(options, options.TurboChatBehavior);
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

        private async Task AddMessagesFromModelAsync()
        {
            foreach (var message in _viewModel.Messages.OrderBy(m => m.Order))
            {
                StringBuilder segments = new();

                message.Segments = message.Segments.OrderBy(s => s.SegmentOrderStart).ToList();

                foreach (var t in message.Segments)
                {
                    segments.AppendLine(t.Content);
                }

                switch (message.Segments[0].Author)
                {
                    case IdentifierEnum.FunctionCall:
                        apiChat.AppendFunctionCall(JsonSerializer.Deserialize<FunctionRequest>(message.Segments[0].Content));
                        break;
                    case IdentifierEnum.FunctionRequest:
                        apiChat.AppendUserInput(message.Segments[0].Content);
                        break;
                    case IdentifierEnum.Api:
                        await AddMessagesHtmlAsync(message.Segments[0].Author, segments.ToString(), false);
                        break;
                    default:
                        await AddMessagesHtmlAsync(message.Segments[0].Author, segments.ToString(), false);
                        apiChat.AppendUserInput(segments.ToString());
                        break;
                }
            }

            await _webView!.ExecuteScriptAsync(WebFunctions.RenderMermaid);
            await _webView!.ExecuteScriptAsync(WebFunctions.ScrollToLastResponse);
        }

        private void OnTxtRequestOnPreviewKeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !completionManager.IsShowed)
            {
                if (options.UseEnter)
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

                            if (btnSqlSend.Visibility == Visibility.Visible)
                            {
                                btnSqlSend_Click(null, null);
                            }
                            else if (btnApiSend.Visibility == Visibility.Visible)
                            {
                                btnApiSend_Click(null, null);
                            }
                            else
                            {
                                _ = RequestAsync(RequestType.Request);
                            }
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
                    apiChat.AppendSystemMessage(selectedContextFilesCode);
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

                if (options.MinifyRequests)
                {
                    originalCode = TextFormat.MinifyText(originalCode, " ");
                }

                originalCode = TextFormat.RemoveCharactersFromText(originalCode, options.CharactersToRemoveFromRequests.Split(','));

                apiChat.AppendSystemMessage(options.TurboChatCodeCommand);
                apiChat.AppendUserInput(originalCode);
            }

            var requestToShowOnList = txtRequest.Text;

            if (attachedImage != null)
            {
                requestToShowOnList = TAG_IMG + txtImage.Text + Environment.NewLine + Environment.NewLine + requestToShowOnList;

                List<ChatContentForImage> chatContent = [new(attachedImage)];

                apiChat.AppendUserInput(chatContent);
            }

            var request = await completionManager.ReplaceReferencesAsync(txtRequest.Text);

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
        private async Task RequestAsync(RequestType commandType, string request, string requestToShowOnList, bool shiftKeyPressed)
        {
            var firstMessage = !_viewModel.Messages.Any();
            apiChat.UpdateApi(options.ApiKey, options.BaseAPI, options.Model);
            await ExecuteRequestWithCommonHandlingAsync(async () =>
            {
                await AddMessagesHtmlAsync(IdentifierEnum.Me, requestToShowOnList);

                request = options.MinifyRequests ? TextFormat.MinifyText(request, " ") : request;

                request = TextFormat.RemoveCharactersFromText(request, options.CharactersToRemoveFromRequests.Split(','));

                apiChat.AppendUserInput(request);

                Application.Current.Dispatcher.Invoke(() => { EnableDisableButtons(false); });

                _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.Me, Content = requestToShowOnList });

                cancellationTokenSource = new();

                if (options.CompletionStream)
                {
                    var gptMessage = _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.ChatGPT });
                    var chatResponses = apiChat.StreamResponseEnumerableFromChatbotAsync(cancellationTokenSource.Token);
                    await foreach (var fragment in chatResponses)
                    {
                        var seg = gptMessage.Segments.First();
                        seg.Content += fragment;
                        await _webView?.ExecuteScriptAsync(WebFunctions.UpdateLastGpt(seg.Content))!;
                    }

                    await HandleFunctionsCallsAsync(apiChat.StreamFunctionResults, cancellationTokenSource);
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

                await _webView?.ExecuteScriptAsync(WebFunctions.RenderMermaid)!;

                if (firstMessage)
                {
                    var request =
                        "Please suggest a concise and relevant title for my first message based on its context, using up to three words and in the same language as my first message.";

                    await UpdateHeaderAsync(request, cancellationTokenSource);
                }

                ChatRepository.UpdateMessages(_viewModel.ChatId, _viewModel.Messages);
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
        private async Task UpdateHeaderAsync(string request, CancellationTokenSource cancellationToken)
        {
            apiChat.AppendUserInput(request);

            (string, List<FunctionResult>) result = await SendRequestAsync(cancellationToken);

            string chatName = Regex.Replace(result.Item1, @"^<think>.*<\/think>", "", RegexOptions.Singleline);

            chatName = TextFormat.RemoveCharactersFromText(chatName, "\r\n", "\n", "\r", ".", ",", ":", ";", "'", "\"").Trim('*');

            string[] words = chatName.Split(' ');

            if (words.Length > 3)
            {
                chatName = string.Concat(words[0], " ", words[1]);
            }

            _viewModel.UpdateChatHeader(chatName);
        }

        /// <summary>
        /// Handles the response based on the command type and shift key state, updating the document view or chat list control items accordingly.
        /// </summary>
        private async void HandleResponse(RequestType commandType, bool shiftKeyPressed, string response)
        {
            if (commandType == RequestType.Code && !shiftKeyPressed)
            {
                var segments = TextFormat.GetChatTurboResponseSegments(response);

                foreach (var t in segments)
                {
                    if (t.Author == IdentifierEnum.ChatGPTCode && docView?.TextView != null)
                    {
                        docView.TextView.TextBuffer.Replace(new Span(0, docView.TextView.TextBuffer.CurrentSnapshot.Length), t.Content);
                    }
                    else
                    {
                        await AddMessagesHtmlAsync(t.Author, t.Content);
                    }
                }
            }
            else
            {
                _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.ChatGPT, Content = response });
                await AddMessagesHtmlAsync(IdentifierEnum.ChatGPT, response);
            }
        }

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
                if (ApiAgent.GetApiFunctions().Select(f => f.Function.Name).Any(f => f.Equals(function.Function.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    (string, string) apiResponse = await ApiAgent.ExecuteFunctionAsync(function, options.LogAPIAgentRequestAndResponses);

                    functionResult = apiResponse.Item1;

                    if (!string.IsNullOrWhiteSpace(apiResponse.Item2))
                    {
                        _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.Api, Content = apiResponse.Item2 });
                        await AddMessagesHtmlAsync(IdentifierEnum.Api, apiResponse.Item2);
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
        /// Adds a formatted HTML message to the message list, including the author's avatar and the response content converted from Markdown.
        /// </summary>
        /// <param name="author">The author of the message, used to determine the avatar image.</param>
        /// <param name="content">The message content in Markdown format to be converted and displayed.</param>
        /// <param name="scrollToBottom">Scroll to bottom.</param>
        private async Task AddMessagesHtmlAsync(IdentifierEnum author, string content, bool scrollToBottom = true)
        {
            var script = author == IdentifierEnum.Me
                ? WebFunctions.AddMsg(content, scrollToBottom)
                : WebFunctions.UpdateLastGpt(content, scrollToBottom);
            await _webView!.ExecuteScriptAsync(script);
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
                    await AddMessagesHtmlAsync(IdentifierEnum.ChatGPT, message.Text);

                    _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.ChatGPT, Content = message.Text });
                }

                // 2. Find the next computer_call action
                ComputerUseOutputItem computerCall = response.Output.FirstOrDefault(o => o.Type == ComputerUseOutputItemType.computer_call);

                if (computerCall == null)
                {
                    // No more actions to perform
                    if (firstMessage)
                    {
                        string request =
                            $"Please suggest a concise and relevant title for the message \"{_viewModel.Messages[0].Segments[0].Content}\", based on its context, using up to three words and in the same language as the message.";

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

                await AddMessagesHtmlAsync(IdentifierEnum.Me, request);

                _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.Me, Content = request });

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
        private void btnSql_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sqlServerConnections = SqlServerAgent.GetConnections();

                if (sqlServerConnections == null || sqlServerConnections.Count == 0)
                {
                    MessageBox.Show("No SQL Server connections were found. Please add connections first through the Server Explorer window.", Constants.EXTENSION_NAME,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                sqlServerConnections = sqlServerConnections.Where(c1 => _viewModel.SqlServerConnectionsAlreadyAdded.All(c2 => c2 != c1.ConnectionString)).ToList();

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
                ChatRepository.DeleteConnectionString(_viewModel.ChatId);

                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return;
            }

            List<FunctionRequest> sqlFunctions = SqlServerAgent.GetSqlFunctions();

            foreach (FunctionRequest function in sqlFunctions)
            {
                apiChat.AppendFunctionCall(function);
                _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.FunctionCall, Content = JsonSerializer.Serialize(function) });
            }

            string request = options.SqlServerAgentCommand + Environment.NewLine + dataBaseSchema + Environment.NewLine;

            SqlServerConnectionInfo connection = (SqlServerConnectionInfo)cbConnection.SelectedItem;

            string requestToShowOnList = TAG_SQL + connection.Description + Environment.NewLine + Environment.NewLine + options.SqlServerAgentCommand;

            _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.FunctionRequest, Content = request });

            await RequestAsync(RequestType.Request, request, requestToShowOnList, false);

            _viewModel.SqlServerConnectionsAlreadyAdded.Add(connection.ConnectionString);

            ChatRepository.AddSqlServerConnection(_viewModel.ChatId, connection.ConnectionString);

            sqlServerConnections.Remove(connection);

            cbConnection.ItemsSource = sqlServerConnections;

            cbConnection.SelectedIndex = 0;

            grdSQL.Visibility = Visibility.Collapsed;
            grdCommands.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles the click event for the SQL Cancel button. Cancels the current request and toggles the visibility of the SQL and Commands grids.
        /// </summary>
        private void btnSqlCancel_Click(object sender, RoutedEventArgs e)
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
        private void btnAPI_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                apiDefinitions = ApiAgent.GetAPIsDefinitions();

                if (apiDefinitions == null || apiDefinitions.Count == 0)
                {
                    MessageBox.Show("No API definitions were found. Please add API definitions first through the extension's options window.", Constants.EXTENSION_NAME,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                apiDefinitions = apiDefinitions.Where(c1 => !_viewModel.ApiDefinitionsAlreadyAdded.Any(c2 => c2 == c1.Name)).ToList();

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
                _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.FunctionCall, Content = JsonSerializer.Serialize(function) });
            }

            ApiItem apiDefinition = (ApiItem)cbAPIs.SelectedItem;

            string request = string.Concat(options.APIAgentCommand, Environment.NewLine, "API Name: ", apiDefinition.Name, Environment.NewLine,
                TextFormat.MinifyText(apiDefinition.Definition, string.Empty));

            string requestToShowOnList = TAG_API + apiDefinition.Name + Environment.NewLine + Environment.NewLine + options.APIAgentCommand;

            _viewModel.AddMessageSegment(new() { Author = IdentifierEnum.FunctionRequest, Content = request });

            await RequestAsync(RequestType.Request, request, requestToShowOnList, false);

            _viewModel.ApiDefinitionsAlreadyAdded.Add(apiDefinition.Name);

            ChatRepository.AddApiDefinition(_viewModel.ChatId, apiDefinition.Name);

            apiDefinitions.Remove(apiDefinition);

            cbAPIs.ItemsSource = apiDefinitions;

            cbAPIs.SelectedIndex = 0;

            grdAPI.Visibility = Visibility.Collapsed;
            grdCommands.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles the click event for the API cancel button. Cancels the current request and updates the UI by hiding the API grid and showing the commands grid.
        /// </summary>
        private void btnApiCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelRequest(sender, e);

            grdAPI.Visibility = Visibility.Collapsed;
            grdCommands.Visibility = Visibility.Visible;
        }

        #endregion API Event Handlers
    }
}

