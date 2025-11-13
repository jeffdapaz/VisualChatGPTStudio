using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JeffPires.VisualChatGPTStudio.Agents;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;
using JeffPires.VisualChatGPTStudio.Utils;
using JeffPires.VisualChatGPTStudio.Utils.API;
using JeffPires.VisualChatGPTStudio.Utils.CodeCompletion;
using JeffPires.VisualChatGPTStudio.Utils.Repositories;
using Microsoft.VisualStudio.Text;
using OpenAI_API.Chat;
using OpenAI_API.Functions;
using OpenAI_API.ResponsesAPI.Models.Request;
using OpenAI_API.ResponsesAPI.Models.Response;
using VisualChatGPTStudioShared.Agents.ApiAgent;
using Toolkit = Community.VisualStudio.Toolkit;
using VS = Microsoft.VisualStudio.Shell;
using AvalonDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace VisualChatGPTStudioShared.ToolWindows.Turbo;

public sealed class TerminalTurboViewModel : INotifyPropertyChanged
{
    private string _search = string.Empty;
    private List<ChatEntity> _filtered = [];
    private int _page;
    private const int PageSize = 10;

    private Conversation _apiChat;

    private Toolkit.DocumentView _docView;
    private Rectangle _screenBounds;
    private string _previousResponseId;

    private bool _selectedContextFilesCodeAppended;

    private bool _shiftKeyPressed;

    private int _toolCallAttempt;

    public TerminalTurboViewModel()
    {
        Messages.CollectionChanged += MessagesOnCollectionChanged;
    }

    public OptionPageGridGeneral Options;
    public VS.Package Package;

    private AvalonDocument _requestDoc = new();

    public AvalonDocument RequestDoc
    {
        get => _requestDoc;
        set => SetField(ref _requestDoc, value);
    }

    private string _attachedImageText = string.Empty;

    public string AttachedImageText
    {
        get => _attachedImageText;
        set => SetField(ref _attachedImageText, value);
    }

    private bool _isReadyToRequest;

    public bool IsReadyToRequest
    {
        get => _isReadyToRequest;
        set
        {
            SetField(ref _isReadyToRequest, value);
            OnPropertyChanged(nameof(IsRequestInProgress));
        }
    }

    private string _progressStatus = string.Empty;

    public string ProgressStatus
    {
        get => _progressStatus;
        private set => SetField(ref _progressStatus, value);
    }

    private bool _useSqlTools;

    public bool UseSqlTools
    {
        get => _useSqlTools;
        set
        {
            if (value)
            {
                SqlServerConnections = SqlServerAgent.GetConnections();

                if (SqlServerConnections.Count != 0)
                {
                    SqlServerConnectionSelectedIndex = 0;
                    UseApiTools = false;
                }
                else
                {
                    SqlServerConnectionSelectedIndex = -1;
                    value = false;
                    MessageBox.Show("No SQL Server connections were found. Please add connections first through the Server Explorer window.", Constants.EXTENSION_NAME,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            SetField(ref _useSqlTools, value);
        }
    }

    private List<SqlServerConnectionInfo> _sqlServerConnections = [];

    public List<SqlServerConnectionInfo> SqlServerConnections
    {
        get => _sqlServerConnections;
        set => SetField(ref _sqlServerConnections, value);
    }

    private int _sqlServerConnectionSelectedIndex = -1;

    public int SqlServerConnectionSelectedIndex
    {
        get => _sqlServerConnectionSelectedIndex;
        set => SetField(ref _sqlServerConnectionSelectedIndex, value);
    }

    private bool _useApiTools;

    public bool UseApiTools
    {
        get => _useApiTools;
        set
        {
            if (value)
            {
                ApiDefinitions = ApiAgent.GetAPIsDefinitions();

                if (ApiDefinitions.Count != 0)
                {
                    ApiDefinitionSelectedIndex = 0;
                    UseSqlTools = false;
                }
                else
                {
                    ApiDefinitionSelectedIndex = -1;
                    value = false;
                    MessageBox.Show("No API definitions were found. Please add API definitions first through the extension's options window.", Constants.EXTENSION_NAME,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            SetField(ref _useApiTools, value);
        }
    }

    private List<ApiItem> _apiDefinitions = [];

    public List<ApiItem> ApiDefinitions
    {
        get => _apiDefinitions;
        set => SetField(ref _apiDefinitions, value);
    }

    private int _apiDefinitionSelectedIndex = -1;

    public int ApiDefinitionSelectedIndex
    {
        get => _apiDefinitionSelectedIndex;
        set => SetField(ref _apiDefinitionSelectedIndex, value);
    }

    /// <summary>
    /// Inverse value <see cref="IsReadyToRequest"/>
    /// </summary>
    public bool IsRequestInProgress => !_isReadyToRequest;

    public CancellationTokenSource CancellationTokenSource;

    public CompletionManager CompletionManager;

    /// <summary>
    /// Chat list from Database
    /// </summary>
    private List<ChatEntity> AllChats { get; set; } = [];

    public byte[] AttachedImage;

    public event Func<string, Task<string>> ScriptRequested;

    private async Task RunScriptAsync(string script)
    {
        if (ScriptRequested is null)
            return;
        await ScriptRequested.Invoke(script);
    }

    /// <summary>
    /// Current ChatId
    /// </summary>
    public string ChatId { get; private set; }

    private ObservableCollection<MessageEntity> Messages { get; } = [];

    public ObservableCollection<ChatEntity> Chats { get; } = [];

    public int PageNumber => _page + 1;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((_filtered?.Count ?? 0.0) / PageSize));

    public string CurrentPageView => $"{PageNumber} / {TotalPages}";

    public bool CanGoPrev => _page > 0;

    public bool CanGoNext => _page < TotalPages - 1;

    public ICommand NextCmd => new RelayCommand(() =>
        {
            _page++;
            ApplyFilter();
        },
        () => CanGoNext);

    public ICommand PrevCmd => new RelayCommand(() =>
        {
            _page--;
            ApplyFilter();
        },
        () => CanGoPrev);

    public ICommand DeleteCmd =>
        new RelayCommand<ChatEntity>(chat => DeleteChat(chat.Id));

    public ICommand StartRenameCmd =>
        new RelayCommand<ChatEntity>(chat =>
        {
            if (chat == null) return;
            chat.EditName = chat.Name;
            chat.IsEditing = !chat.IsEditing;
        });

    public void DeleteChat(string delId)
    {
        if (string.IsNullOrEmpty(delId))
            return;
        ChatRepository.DeleteChat(delId);
        AllChats.Remove(AllChats.FirstOrDefault(c => c.Id == delId));
        ApplyFilter();

        if (delId == ChatId)
        {
            ChatId = string.Empty;
            Messages.Clear();
            _apiChat.ClearConversation();

            UseApiTools = UseSqlTools = false;
            AttachedImage = null;
        }
    }

    public string Search
    {
        get => _search;
        set
        {
            if (SetField(ref _search, value))
                ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        if (!string.IsNullOrEmpty(Search))
        {
            _filtered = AllChats
                .Where(c => c.Name.ToLower().Contains(Search.ToLower()))
                .ToList();
        }
        else
        {
            _filtered = AllChats;
        }

        if (_page >= TotalPages)
        {
            _page = TotalPages - 1;
        }

        SyncCurrentPage();
        UpdatePaginationProperties();
    }

    private void UpdatePaginationProperties()
    {
        OnPropertyChanged(nameof(PageNumber));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CurrentPageView));
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(CanGoNext));
    }

    private void SyncCurrentPage()
    {
        var itemsOnPage = _filtered;
        if (itemsOnPage.Count > PageSize)
        {
            var itemsToSkip = _page * PageSize;
            itemsOnPage = _filtered.Skip(itemsToSkip).Take(PageSize).ToList();
        }

        var newItems = itemsOnPage ?? [];
        for (var i = Chats.Count - 1; i >= 0; i--)
        {
            if (!newItems.Contains(Chats[i]))
            {
                Chats.RemoveAt(i);
            }
        }

        for (var i = 0; i < newItems.Count; i++)
        {
            if (i < Chats.Count)
            {
                if (!Chats[i].Equals(newItems[i]))
                {
                    Chats[i] = newItems[i];
                }
                else
                {
                    Chats[i].OnAllPropertiesChanged();
                }
            }
            else
            {
                Chats.Add(newItems[i]);
            }
        }
    }

    private void DownloadChats()
    {
        AllChats = ChatRepository.GetChats();
        foreach (var c in AllChats)
        {
            c.IsSelected = ChatId == c.Id;
        }
    }

    private void AddMessageSegment(ChatMessageSegment segment)
    {
        if (string.IsNullOrEmpty(segment.Content))
        {
            return;
        }
        if (string.IsNullOrEmpty(ChatId))
        {
            CreateNewChat(clearTools: false);
        }
        Messages.Add(new MessageEntity { Order = Messages.Count + 1, Segments = [ segment ] });
    }

    private void UpdateChatHeader(string header)
    {
        ChatRepository.UpdateChatName(ChatId, header);
        var entity = Chats.FirstOrDefault(c => c.Id == ChatId);
        if (entity != null)
        {
            entity.Name = header;
            entity.EditName = header;
        }
    }

    public void CreateNewChat(bool clearTools)
    {
        ChatRepository.AddChat(new ChatEntity
        {
            Id = Guid.NewGuid().ToString(),
            Date = DateTime.Now,
            Messages = [],
            Name = $"New chat {AllChats.Count + 1}"
        });

        if (clearTools)
        {
            UseApiTools = UseSqlTools = false;
            AttachedImage = null;
        }

        ForceDownloadChats();
        LoadChat();
    }

    public void ForceDownloadChats()
    {
        DownloadChats();
        ApplyFilter();
    }

    public void LoadChat(string id = "")
    {
        var selectedChat = string.IsNullOrEmpty(id)
            ? AllChats.FirstOrDefault()
            : AllChats.FirstOrDefault(c => c.Id == id);

        ChatId = selectedChat?.Id ?? string.Empty;
        Messages.Clear();
        if (!string.IsNullOrEmpty(ChatId))
        {
            foreach (var message in ChatRepository.GetMessages(ChatId).OrderBy(m => m.Order))
            {
                Messages.Add(message);
            }
        }

        _apiChat = ApiHandler.CreateConversation(Options, Options.TurboChatBehavior);
        foreach (var messageEntity in Messages)
        {
            var segment = messageEntity.Segments.FirstOrDefault();
            // tools should be a response to a previous message using tool_calls
            // But we are not saving tool_calls in Assistant
            if (segment == null || segment.Author is IdentifierEnum.Table or IdentifierEnum.FunctionRequest or IdentifierEnum.FunctionCall)
            {
                continue;
            }
            _apiChat.Messages.Add(new ChatMessage
                {
                    Role = segment.Author switch
                    {
                        IdentifierEnum.Me => ChatMessageRole.User,
                        IdentifierEnum.ChatGPT or IdentifierEnum.ChatGPTCode => ChatMessageRole.Assistant,
                        _ => ChatMessageRole.System
                    },
                    Content = segment.Content
                }
            );
        }

        IsReadyToRequest = true;
    }

    private readonly JsonSerializerOptions _serializeOptions = new()
    {
        WriteIndented = true,
        MaxDepth = 10,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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
        if (functions.Count == 0)
        {
            return false;
        }

        foreach (var function in functions)
        {
            // TODO: Approve before to calling (auto or manual)
            string functionResult;
            if (ApiAgent.GetApiFunctions().Select(f => f.Function.Name).Any(f => f.Equals(function.Function.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                (string FunctionResult, string Content) apiResponse = await ApiAgent.ExecuteFunctionAsync(function, Options.LogAPIAgentRequestAndResponses);

                functionResult = apiResponse.FunctionResult;

                if (!string.IsNullOrWhiteSpace(apiResponse.Content))
                {
                    functionResult = apiResponse.Content;
                }
            }
            else
            {
                functionResult = SqlServerAgent.ExecuteFunction(function, Options.LogSqlServerAgentQueries, out var rows);
                if (rows is { Count: > 0 })
                {
                    // Showing the selected result in chat without sending it to LLM
                    AddMessageSegment(new ChatMessageSegment { Author = IdentifierEnum.Table, Content = JsonSerializer.Serialize(rows, _serializeOptions) });
                }
            }

            _apiChat.AppendToolMessage(function, functionResult);
            function.Function.Result = functionResult;
            AddMessageSegment(new ChatMessageSegment { Author = IdentifierEnum.FunctionCall, Content = JsonSerializer.Serialize(function.Function, _serializeOptions) });
        }

        if (_toolCallAttempt >= Options.ToolCallMaxAttempts)
        {
            // Preventing an endless loop of calling tools
            // No tools - no calls ¯\_(ツ)_/¯
            _apiChat.ClearTools();
        }

        (string Content, List<FunctionResult> ListFunctions) result = await SendRequestAsync(cancellationToken);

        var responseHandled = false;
        if (result.ListFunctions != null && result.ListFunctions.Any())
        {
            responseHandled = await HandleFunctionsCallsAsync(result.ListFunctions, cancellationToken);
        }

        if (!responseHandled)
        {
            AddMessageSegment(new() { Author = IdentifierEnum.ChatGPT, Content = result.Content });
        }

        return true;
    }

    private bool IsReadyToSendRequest()
    {
        if (!Options.AzureEntraIdAuthentication && string.IsNullOrWhiteSpace(Options.ApiKey))
        {
            MessageBox.Show(Constants.MESSAGE_SET_API_KEY, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            Package.ShowOptionPage(typeof(OptionPageGridGeneral));
            return false;
        }

        if (string.IsNullOrWhiteSpace(RequestDoc.Text))
        {
            MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles an asynchronous request based on the specified command type. Validates input, appends context or system messages, processes code or image-related requests, and sends the final request.
    /// </summary>
    public async Task RequestAsync(RequestType commandType)
    {
        _shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        if (!IsReadyToSendRequest())
        {
            return;
        }

        if (!_selectedContextFilesCodeAppended)
        {
            var selectedContextFilesCode = await GetSelectedContextItemsCodeAsync();

            if (!string.IsNullOrWhiteSpace(selectedContextFilesCode))
            {
                _apiChat.AppendSystemMessage(selectedContextFilesCode);
                _selectedContextFilesCodeAppended = true;
            }
        }

        var requestToShowOnList = RequestDoc.Text;
        string request;

        if (commandType == RequestType.Code)
        {
            _docView = await Toolkit.VS.Documents.GetActiveDocumentViewAsync();
            var originalCode = _docView?.TextView?.TextBuffer?.CurrentSnapshot.GetText();
            if (originalCode == null)
            {
                Logger.Log("Code is null");
                return;
            }

            if (Options.MinifyRequests)
            {
                originalCode = TextFormat.MinifyText(originalCode, " ");
            }

            originalCode = TextFormat.RemoveCharactersFromText(originalCode, Options.CharactersToRemoveFromRequests.Split(','));

            _apiChat.AppendSystemMessage(Options.TurboChatCodeCommand);

            request = $"""
                       <task>{requestToShowOnList}</task>
                       <code>
                       {originalCode}
                       </code>
                       """;
        }
        else
        {
            request = await CompletionManager.ReplaceReferencesAsync(requestToShowOnList);
        }

        // TODO make renaming chat is optionable + manual call
        var firstMessage = !Messages.Any();

        // TODO update baseUrl for azure.com
        if (Options.Service == OpenAIService.OpenAI)
        {
            _apiChat.UpdateOpenAiApi(Options.ApiKey, Options.BaseAPI);
            if (!string.IsNullOrEmpty(Options.Model))
            {
                _apiChat.RequestParameters.Model = Options.Model;
            }
        }

        _toolCallAttempt = 0;

        await ExecuteRequestWithCommonHandlingAsync(async () =>
        {
            AddMessageSegment(new() { Author = IdentifierEnum.Me, Content = requestToShowOnList });
            RequestDoc.Text = string.Empty;

            request = Options.MinifyRequests ? TextFormat.MinifyText(request, " ") : request;
            request = TextFormat.RemoveCharactersFromText(request, Options.CharactersToRemoveFromRequests.Split(','));
            request = UpdateTools(request);

            if (AttachedImage != null)
            {
                // send image with request
                List<object> chatContent = [
                    new ChatContentForImage(AttachedImage),
                    new { type = "text", text = request }
                ];
                _apiChat.AppendUserInput(chatContent);
            }
            else
            {
                _apiChat.AppendUserInput(request);
            }

            CancellationTokenSource = new();
            var segment = new ChatMessageSegment { Author = IdentifierEnum.ChatGPT, Content = "" };
            List<FunctionResult> resultTools;

            if (Options.CompletionStream) // stream
            {
                AddMessageSegment(segment);
                var content = new StringBuilder();
                var chatResponses = _apiChat.StreamResponseEnumerableFromChatbotAsync(CancellationTokenSource.Token);
                await foreach (var fragment in chatResponses)
                {
                    content.Append(fragment);
                    // TODO: send fragment instead full content
                    if (content.Length > 0) // dont show empty bubble
                    {
                        await RunScriptAsync(WebFunctions.UpdateLastGpt(content.ToString()))!;
                    }
                }

                segment.Content = content.ToString();
                resultTools = _apiChat.StreamFunctionResults;
            }
            else // non-stream
            {
                (segment.Content, resultTools) = await SendRequestAsync(CancellationTokenSource);
                AddMessageSegment(segment);
            }

            if (resultTools != null && resultTools.Any())
            {
                await HandleFunctionsCallsAsync(resultTools, CancellationTokenSource);
            }
            else if (commandType == RequestType.Code && !_shiftKeyPressed)
            {
                HandleCodeResponse(segment);
            }

            // restore request in message history
            _apiChat.ReplaceLastUserInput(requestToShowOnList);
            await RunScriptAsync(WebFunctions.RenderMermaid)!;

            if (firstMessage)
            {
                await UpdateHeaderAsync("Please suggest a concise and relevant title for my first message based on its context, " +
                                        "using up to five words and in the same language as my first message.");
            }

            ChatRepository.UpdateMessages(ChatId, Messages.ToList());
        });
    }

    public async Task ComputerUseAsync()
    {
        var firstMessage = !Messages.Any();
        await ExecuteRequestWithCommonHandlingAsync(async () =>
        {
            ProgressStatus = "AI is executing actions. Please wait and avoid interaction until completion.";

            byte[] screenshot = ScreenCapturer.CaptureFocusedScreenScreenshot(out _screenBounds);

            string request = RequestDoc.Text;
            RequestDoc.Text = string.Empty;

            AddMessageSegment(new() { Author = IdentifierEnum.Me, Content = request });

            CancellationTokenSource = new();

            Task<ComputerUseResponse> task;

            if (!string.IsNullOrWhiteSpace(_previousResponseId))
            {
                task = ApiHandler.GetComputerUseResponseAsync(Options, request, _screenBounds.Width, _screenBounds.Height, _previousResponseId, CancellationTokenSource.Token);
            }
            else
            {
                task = ApiHandler.GetComputerUseResponseAsync(Options, request, _screenBounds.Width, _screenBounds.Height, screenshot, CancellationTokenSource.Token);
            }

            await Task.WhenAny(task, Task.Delay(Timeout.Infinite, CancellationTokenSource.Token));

            CancellationTokenSource.Token.ThrowIfCancellationRequested();

            ComputerUseResponse response = await task;

            await ProcessComputerUseResponseAsync(response, firstMessage);
        });
    }

    /// <summary>
    /// Processes the computer use response asynchronously by displaying messages, executing computer actions,
    /// capturing screenshots, and sending subsequent requests until no further actions are available.
    /// </summary>
    /// <param name="response">The initial ComputerUseResponse containing output items to process.</param>
    /// <param name="firstMessage">The start of new chat.</param>
    private async Task ProcessComputerUseResponseAsync(ComputerUseResponse response, bool firstMessage)
    {
        while (true)
        {
            _previousResponseId = response.Id;

            // 1. Display messages and reasoning in the UI
            List<ComputerUseContent> messages = response.Output
                .Where(o => o.Type == ComputerUseOutputItemType.reasoning && o.Summary != null)
                .SelectMany(o => o.Summary).ToList();

            messages.AddRange(response.Output
                .Where(o => o.Type == ComputerUseOutputItemType.message && o.Content != null)
                .SelectMany(o => o.Content));

            foreach (ComputerUseContent message in messages)
            {
                AddMessageSegment(new() { Author = IdentifierEnum.ChatGPT, Content = message.Text });
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
                        $" \"{Messages[0].Segments[0].Content}\"," +
                        $" based on its context, using up to four-five words and in the same language as the message.";

                    await UpdateHeaderAsync(request);
                }
                break;
            }

            // 3. Execute the action programmatically in Windows/Visual Studio
            await ComputerUse.DoActionAsync(computerCall.Action, _screenBounds);

            // 4. Capture the updated screenshot
            byte[] screenshot = ScreenCapturer.CaptureFocusedScreenScreenshot(out _screenBounds);

            // 5. Send the next request using the output of the action
            response = await ApiHandler.GetComputerUseResponseAsync(
                Options,
                _screenBounds.Width,
                _screenBounds.Height,
                screenshot,
                computerCall.CallId,
                response.Id,
                computerCall.PendingSafetyChecks,
                CancellationTokenSource.Token
            );
        }
    }

    private string UpdateTools(string input)
    {
        var result = input;
        _apiChat.ClearTools();
        if (UseSqlTools && SqlServerConnectionSelectedIndex != -1 && SqlServerConnections.Count > SqlServerConnectionSelectedIndex)
        {
            var sqlServerConnectionInfo = SqlServerConnections[SqlServerConnectionSelectedIndex];
            var dataBaseSchema = SqlServerAgent.GetDataBaseSchema(sqlServerConnectionInfo.ConnectionString);
            var sqlFunctions = SqlServerAgent.GetSqlFunctions();
            foreach (var function in sqlFunctions)
            {
                _apiChat.AppendFunctionCall(function);
            }

            result = $"""
                      {Options.SqlServerAgentCommand}
                      <dataBaseSchema>{dataBaseSchema}</dataBaseSchema>
                      {input}
                      """;
        }
        if (UseApiTools && ApiDefinitionSelectedIndex != -1 && ApiDefinitions.Count > ApiDefinitionSelectedIndex)
        {
            var apiDefinition = ApiDefinitions[ApiDefinitionSelectedIndex];
            var apiFunctions = ApiAgent.GetApiFunctions();
            foreach (var function in apiFunctions)
            {
                _apiChat.AppendFunctionCall(function);
            }

            result = $"""
                      {Options.APIAgentCommand}
                      <apiName>{apiDefinition.Name}</apiName>
                      <apiDefinition>{TextFormat.MinifyText(apiDefinition.Definition, string.Empty)}</apiDefinition>
                      <task>{input}</task>
                      """;
        }

        return result;
    }

    /// <summary>
    /// Executes the specified asynchronous operation with common exception handling for requests.
    /// </summary>
    /// <param name="requestOperation">The asynchronous operation to execute.</param>
    private async Task ExecuteRequestWithCommonHandlingAsync(Func<Task> requestOperation)
    {
        try
        {
            IsReadyToRequest = false;
            ProgressStatus = "Waiting API Response.";
            if (IsReadyToSendRequest())
            {
                await requestOperation();
            }
        }
        catch (OperationCanceledException)
        {
            // catch request cancellation
        }
        catch (Exception ex)
        {
            Logger.Log(ex);
            MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            // remove last user message
            var lastMess = _apiChat.Messages.LastOrDefault();
            if (lastMess != null && lastMess.Role.Equals(ChatMessageRole.User))
            {
                _apiChat.Messages.Remove(lastMess);
                // TODO run a script "RunScriptAsync()" to show that the last message was not sent.
            }
        }
        finally
        {
            IsReadyToRequest = true;
            AttachedImageText = string.Empty;
            AttachedImage = null;
        }
    }

    /// <summary>
    /// Asynchronously gets the code of the selected context items.
    /// </summary>
    /// <returns>The code of the selected context items as a string.</returns>
    private static async Task<string> GetSelectedContextItemsCodeAsync()
    {
        var selectedContextFilesCode = await TerminalWindowSolutionContextCommand.Instance.GetSelectedContextItemsCodeAsync();
        return string.Concat(selectedContextFilesCode);
    }

    /// <summary>
    /// Handles the response based on the command type and shift key state, updating the document view or chat list control items accordingly.
    /// </summary>
    private void HandleCodeResponse(ChatMessageSegment segment)
    {
        var segments = TextFormat.GetChatTurboResponseSegments(segment.Content);
        var stringBuilder = new StringBuilder();

        foreach (var t in segments)
        {
            if (t.Author == IdentifierEnum.ChatGPTCode && _docView?.TextView != null)
            {
                _docView.TextView.TextBuffer.Replace(new Span(0, _docView.TextView.TextBuffer.CurrentSnapshot.Length), t.Content);
            }

            stringBuilder.AppendLine(t.Content);
        }

        segment.Author = segments[0].Author;
        segment.Content = stringBuilder.ToString();
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
        Task<(string, List<FunctionResult>)> task = _apiChat.GetResponseContentAndFunctionAsync();

        await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));

        cancellationTokenSource.Token.ThrowIfCancellationRequested();

        return await task;
    }

    /// <summary>
    /// Updates the chat header based on the user's request by sending the request,
    /// processing the response to generate a chat name, and notifying the parent control of the new chat.
    /// </summary>
    /// <param name="request">The user's input request to be sent and processed.</param>
    private async Task UpdateHeaderAsync(string request)
    {
        _apiChat.MessagesCheckpoint();
        _apiChat.AppendUserInput(request);

        (string, List<FunctionResult>) result = await SendRequestAsync(CancellationTokenSource);

        string chatName = Regex.Replace(result.Item1, @"^<think>.*<\/think>", "", RegexOptions.Singleline);

        chatName = TextFormat.RemoveCharactersFromText(chatName, "\r\n", "\n", "\r", ".", ",", ":", ";", "'", "\"").Trim('*');

        string[] words = chatName.Split(' ');

        if (words.Length > 5)
        {
            chatName = string.Join(" ", words[0], words[1], words[2], words[3]);
        }

        _apiChat.MessagesRestoreFromCheckpoint();
        UpdateChatHeader(chatName);
    }

    private async void MessagesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null || e.NewItems.Count == 0)
        {
           await RunScriptAsync(WebFunctions.ClearChat);
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
                        if (!Options.ShowToolCalls)
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

                    // send image
                    string imageData = null;
                    if (AttachedImage is { Length: > 100 } && !string.IsNullOrEmpty(AttachedImageText))
                    {
                        var mimeType = AttachImage.GetImageMimeType(AttachedImage);
                        var base64String = Convert.ToBase64String(AttachedImage);
                        imageData = $"data:{mimeType};base64,{base64String}";
                    }

                    var script = author switch
                    {
                        IdentifierEnum.ChatGPT => WebFunctions.UpdateLastGpt(content),
                        IdentifierEnum.Table => WebFunctions.AddTable(content),
                        _ => WebFunctions.AddMsg(author, content, imageData)
                    };

                    await RunScriptAsync(script);

                    if (author == IdentifierEnum.ChatGPT)
                    {
                        await RunScriptAsync(WebFunctions.RenderMermaid);
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

    // TODO: toolbar buttons visibility
    public bool IsProcessing { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
