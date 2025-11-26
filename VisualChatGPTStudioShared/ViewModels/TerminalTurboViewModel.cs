using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
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
using System.Windows.Media.Imaging;
using JeffPires.VisualChatGPTStudio.Agents;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.ToolWindows;
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
using JsonSerializer = System.Text.Json.JsonSerializer;

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

    private readonly ToolManager _toolManager = new();

    /// <summary>
    /// Main constructor
    /// </summary>
    public TerminalTurboViewModel()
    {
        _toolManager.ScriptRequested += RunScriptAsync;
        _toolManager.AddBuiltInTools();
    }

    public OptionPageGridGeneral Options;
    public VS.Package Package;

    public AvalonDocument RequestDoc
    {
        get;
        set => SetField(ref field, value);
    } = new();

    public string AttachedImageText
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public bool IsReadyToRequest
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(IsRequestInProgress));
        }
    }

    public string ProgressStatus
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    public bool UseSqlTools
    {
        get;
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
                    value = false;
                    SqlServerConnectionSelectedIndex = -1;
                    MessageBox.Show("No SQL Server connections were found. Please add connections first through the Server Explorer window.", Constants.EXTENSION_NAME,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            SetField(ref field, value);
        }
    }

    public List<SqlServerConnectionInfo> SqlServerConnections
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public int SqlServerConnectionSelectedIndex
    {
        get;
        set
        {
            SetField(ref field, value);
            if (value != -1 && SqlServerConnections.Count > value)
            {
                SqlServerAgent.CurrentConnectionString = SqlServerConnections[value].ConnectionString;
            }
            else
            {
                SqlServerAgent.CurrentConnectionString = null;
            }
        }
    } = -1;

    public bool UseApiTools
    {
        get;
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

            SetField(ref field, value);
        }
    }

    public List<ApiItem> ApiDefinitions
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public int ApiDefinitionSelectedIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    /// <summary>
    /// Inverse value <see cref="IsReadyToRequest"/>
    /// </summary>
    public bool IsRequestInProgress => !IsReadyToRequest;

    private CancellationTokenSource cancellationTokenSource;

    public CompletionManager CompletionManager;

    /// <summary>
    /// Chat list from Database
    /// </summary>
    private List<ChatEntity> AllChats { get; set; } = [];

    public byte[] AttachedImage;

    public event Func<string, Task<string>> ScriptRequested;

    private async Task<string> RunScriptAsync(string script)
    {
        if (ScriptRequested is null)
            return string.Empty;
        return await ScriptRequested.Invoke(script);
    }

    /// <summary>
    /// Current ChatId
    /// </summary>
    public string ChatId { get; private set; }

    private List<MessageEntity> MessagesForUi { get; } = [];

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
            MessagesForUi.Clear();
            _ = RunScriptAsync(WebFunctions.ClearChat);
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

    private async Task AddUiMessageSegmentAsync(ChatMessageSegment segment)
    {
        if (string.IsNullOrEmpty(ChatId))
        {
            await CreateNewChatAsync(clearTools: false);
        }

        var message = new MessageEntity { Order = MessagesForUi.Count + 1, Segments = [segment] };
        MessagesForUi.Add(message);
        await SendMessageToUiAsync(message);
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

    public async Task CreateNewChatAsync(bool clearTools)
    {
        ChatId = Guid.NewGuid().ToString();

        ChatRepository.AddChat(new ChatEntity
        {
            Id = ChatId,
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
        await LoadChatAsync(ChatId);
    }

    public void ForceDownloadChats()
    {
        DownloadChats();
        ApplyFilter();
    }

    public async Task LoadChatAsync(string id = "")
    {
        var selectedChat = string.IsNullOrEmpty(id)
            ? AllChats.FirstOrDefault()
            : AllChats.FirstOrDefault(c => c.Id == id);

        ChatId = selectedChat?.Id ?? string.Empty;
        MessagesForUi.Clear();
        await RunScriptAsync(WebFunctions.ClearChat);
        _toolManager.CancelAllPendingTools();
        if (!string.IsNullOrEmpty(ChatId))
        {
            foreach (var message in ChatRepository.GetMessages(ChatId))
            {
                MessagesForUi.Add(message);
                await SendMessageToUiAsync(message);
            }
        }

        if (_apiChat == null)
        {
            _apiChat = ApiHandler.CreateConversation(Options, GetSystemMessage());
        }
        else
        {
            _apiChat.ClearConversation(GetSystemMessage());
        }

        foreach (var messageEntity in MessagesForUi)
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

    public void CancelRequest()
    {
        IsReadyToRequest = false;
        cancellationTokenSource?.Cancel();
        _toolManager.CancelAllPendingTools();
    }

    private readonly JsonSerializerOptions _serializeOptions = new()
    {
        WriteIndented = true,
        MaxDepth = 10,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private string GetSystemMessage()
    {
        return !Options.UseOnlySystemMessageTools
            ? Options.TurboChatBehavior
            : $"""
               {Options.TurboChatBehavior}

               {_toolManager.GetToolUseSystemInstructions()}
               """;
    }

    private void AppendToolResultToApi(ToolToCall toolToCall)
    {
        if (Options.UseOnlySystemMessageTools)
        {
            _apiChat.AppendMessage(new ChatMessage
            {
                Role = ChatMessageRole.User,
                Name = "Tool",
                Content = $"""
                           {toolToCall.Tool.Name}:

                           {toolToCall.Result?.Result}
                           """
            });
        }
        else
        {
            _apiChat.AppendMessage(new ChatMessage
            {
                Role = ChatMessageRole.Tool,
                Content = toolToCall.Result?.Result,
                FunctionId = toolToCall.CallId
            });
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
        if (functions.Count == 0)
        {
            return false;
        }

        List<ToolToCall> toolsToCall = [];
        foreach (var functionResult in functions)
        {
            var tool = _toolManager.GetTool(functionResult.Function.Name);
            if (tool != null)
            {
                if (tool.Enabled)
                {
                    tool.LogResponseAndRequest = tool.Category switch
                    {
                        "API" => Options.LogAPIAgentRequestAndResponses,
                        "SQL" => Options.LogSqlServerAgentQueries,
                        _ => false
                    };
                    toolsToCall.Add(new ToolToCall
                    {
                        Tool = tool,
                        ArgumentsJson = functionResult.Function.Arguments,
                        CallId = string.IsNullOrEmpty(functionResult.Id)
                            ? Guid.NewGuid().ToString()
                            : functionResult.Id
                    });
                }
                else
                {
                    Logger.Log($"Tool '{functionResult.Function.Name}' is disabled.");
                    _apiChat.Messages.Add(new ChatMessage
                    {
                        Role = ChatMessageRole.System,
                        Content = $"Tool '{functionResult.Function.Name}' is disabled."
                    });
                }
            }
            else
            {
                Logger.Log($"Could not find tool '{functionResult.Function.Name}'.");
                _apiChat.Messages.Add(new ChatMessage
                {
                    Role = ChatMessageRole.System,
                    Content = $"Could not find tool '{functionResult.Function.Name}'."
                });
            }
        }

        ProgressStatus = "Waiting tools approval";
        var approvedTools = await _toolManager.RequestApprovalAsync(toolsToCall);
        foreach (var notApproved in toolsToCall.Except(approvedTools))
        {
            notApproved.Result = new ToolResult { Result = $"Tool '{notApproved.Tool.Name}' isn't approved by user." };
            AppendToolResultToApi(notApproved);
        }
        await foreach (var executedTool in _toolManager.ExecuteToolsAsync(approvedTools))
        {
            ProgressStatus = $"Tool '{executedTool.Tool.Name}' executing...";
            var toolResult = executedTool.Result!;
            if (executedTool.Tool.Category == "API")
            {
                if (!string.IsNullOrWhiteSpace(toolResult.PrivateResult))
                {
                    // Showing the selected result in chat without sending it to LLM
                    await AddUiMessageSegmentAsync(new ChatMessageSegment { Author = IdentifierEnum.Me, Content = $"#API{Environment.NewLine}{toolResult.PrivateResult}" });
                }
            }
            else if (executedTool.Tool.Category == "SQL")
            {
                if (!string.IsNullOrWhiteSpace(toolResult.PrivateResult))
                {
                    // Showing the selected result in chat without sending it to LLM
                    await AddUiMessageSegmentAsync(new ChatMessageSegment { Author = IdentifierEnum.Table, Content = toolResult.PrivateResult });
                }
            }

            AppendToolResultToApi(executedTool);

            await AddUiMessageSegmentAsync(new ChatMessageSegment { Author = IdentifierEnum.FunctionCall, Content = JsonUtils.Serialize(executedTool.Result) });
            _toolCallAttempt++;
        }

        if (_toolCallAttempt >= Options.ToolCallMaxAttempts)
        {
            // Preventing an endless loop of calling tools
            // No tools - no calls ¯\_(ツ)_/¯
            _apiChat.ClearTools();
        }

        ProgressStatus = "Waiting API Response";
        (string Content, List<FunctionResult> ListFunctions) result = await SendRequestAsync(cancellationToken);

        var responseHandled = false;
        if (result.ListFunctions != null && result.ListFunctions.Any())
        {
            responseHandled = await HandleFunctionsCallsAsync(result.ListFunctions, cancellationToken);
        }

        if (!responseHandled)
        {
            await AddUiMessageSegmentAsync(new() { Author = IdentifierEnum.ChatGPT, Content = result.Content });
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

        if (string.IsNullOrWhiteSpace(RequestDoc.Text) && !(!Options.OneShotToolMode && (UseSqlTools || UseApiTools)))
        {
            MessageBox.Show(Constants.MESSAGE_WRITE_REQUEST, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        return IsReadyToRequest;
    }

    private async Task HandleExistingWebMessageTypesAsync(JsonElement root)
    {
        var action = root.GetProperty("action").GetString()?.ToLower();
        var data = root.GetProperty("data").GetString();
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

    public async Task OnFrontMessageReceivedAsync(string messageAsJson)
    {
        using var document = JsonDocument.Parse(messageAsJson);
        var root = document.RootElement;

        if (root.TryGetProperty("type", out var typeElement))
        {
            var messageType = typeElement.GetString();
            switch (messageType)
            {
                case "tool_execute":
                    HandleToolExecuteMessage(root);
                    break;
                case "tool_cancelled":
                    HandleToolCancelledMessage(root);
                    break;
                case "private_result":
                    HandlePrivateResultMessage(root);
                    break;
                default:
                    await HandleExistingWebMessageTypesAsync(root);
                    break;
            }
        }
        else
        {
            await HandleExistingWebMessageTypesAsync(root);
        }
    }

    private void HandleToolExecuteMessage(JsonElement message)
    {
        if (message.TryGetProperty("tool_id", out var toolIdElement) &&
            message.TryGetProperty("parameters", out var parametersElement))
        {
            var toolId = toolIdElement.GetString();
            var parameters = ParseParameters(parametersElement);

            if (toolId != null)
            {
                _toolManager.ApproveTool(toolId, parameters);
            }
        }
    }

    private void HandleToolCancelledMessage(JsonElement message)
    {
        if (message.TryGetProperty("tool_id", out var toolIdElement))
        {
            var toolId = toolIdElement.GetString();
            var reason = message.TryGetProperty("reason", out var reasonElement)
                ? reasonElement.GetString() ?? "user_cancelled"
                : "user_cancelled";

            if (toolId != null)
            {
                _toolManager.CancelTool(toolId, reason);
            }
        }
    }

    private void HandlePrivateResultMessage(JsonElement message)
    {
        if (message.TryGetProperty("tool_name", out var toolNameElement) &&
            message.TryGetProperty("result", out var resultElement))
        {
            var toolName = toolNameElement.GetString();
            var result = resultElement.GetString();

            if (!string.IsNullOrEmpty(result))
            {
                Logger.Log($"Private result from {toolName}: {result}");
            }
        }
    }

    private IReadOnlyDictionary<string, object> ParseParameters(JsonElement el)
    {
        try
        {
            if (el.ValueKind != JsonValueKind.Object)
                throw new JsonException("parameters must be a JSON object");

            return (IReadOnlyDictionary<string, object>)ParseToken(el);
        }
        catch (Exception ex)
        {
            var raw = el.GetRawText();
            Logger.Log($"Failed to parse parameters JSON: {raw} - {ex.Message}");
            return new Dictionary<string, object>();
        }
    }

    private static object ParseToken(JsonElement token)
    {
        try
        {
            return token.ValueKind switch
            {
                JsonValueKind.String  => token.GetString()!,
                JsonValueKind.True    => true,
                JsonValueKind.False   => false,
                JsonValueKind.Number  => token.TryGetInt64(out var l) ? l : token.GetDouble(),
                JsonValueKind.Array   => token.EnumerateArray().Select(ParseToken).ToList(),
                JsonValueKind.Object  => token.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ParseToken(p.Value)),
                _ => throw new NotSupportedException($"Unsupported JSON token '{token.ValueKind}'")
            };
        }
        catch (Exception inner)
        {
            throw new JsonException($"Error at path '{token}' – {inner.Message}", inner);
        }
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

        try
        {
            if (!_selectedContextFilesCodeAppended)
            {
                var selectedContextFilesCode = await GetSelectedContextItemsCodeAsync();

                if (!string.IsNullOrWhiteSpace(selectedContextFilesCode))
                {
                    _apiChat.AppendSystemMessage(selectedContextFilesCode);
                    _selectedContextFilesCodeAppended = true;
                }
            }

            _apiChat.UseOnlySystemMessageTools = Options.UseOnlySystemMessageTools;
            _apiChat.UpdateFirstSystemMessage(GetSystemMessage());

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

            var firstMessage = !MessagesForUi.Any();

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
                RequestDoc.Text = string.Empty;

                request = Options.MinifyRequests ? TextFormat.MinifyText(request, " ") : request;
                request = TextFormat.RemoveCharactersFromText(request, Options.CharactersToRemoveFromRequests.Split(','));
                UpdateNativeTools(ref request, ref requestToShowOnList);

                await AddUiMessageSegmentAsync(new() { Author = IdentifierEnum.Me, Content = requestToShowOnList });
                var apiUserMessage = new ChatMessage
                {
                    Role = ChatMessageRole.User,
                    Content = AttachedImage == null
                        ? request
                        : new List<object>
                        {
                            new ChatContentForImage(AttachedImage), // send image with request
                            new { type = "text", text = request }
                        }
                };
                _apiChat.Messages.Add(apiUserMessage);

                cancellationTokenSource = new();
                var segment = new ChatMessageSegment { Author = IdentifierEnum.ChatGPT, Content = "" };
                List<FunctionResult> resultTools;

                if (Options.CompletionStream) // stream
                {
                    await AddUiMessageSegmentAsync(segment);
                    var content = new StringBuilder();
                    var chatResponses = _apiChat.StreamResponseEnumerableFromChatbotAsync(cancellationTokenSource.Token);
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
                    (segment.Content, resultTools) = await SendRequestAsync(cancellationTokenSource);
                    await AddUiMessageSegmentAsync(segment);
                }

                if (resultTools != null && resultTools.Any())
                {
                    segment.Content = "";
                    await RunScriptAsync(WebFunctions.RemoveLastGpt);
                    await HandleFunctionsCallsAsync(resultTools, cancellationTokenSource);
                }
                else if (commandType == RequestType.Code && !_shiftKeyPressed)
                {
                    HandleCodeResponse(segment);
                }

                if (Options.OneShotToolMode && AttachedImage == null)
                {
                    // restore user request in message history
                    apiUserMessage.Content = requestToShowOnList;
                }

                await RunScriptAsync(WebFunctions.RenderMermaid)!;

                if (firstMessage && Options.AutoRenameChats)
                {
                    await UpdateHeaderAsync("Please suggest a concise and relevant title for my first message based on its context, " +
                                            "using up to five words and in the same language as my first message.");
                }

                ChatRepository.UpdateMessages(ChatId, MessagesForUi.ToList());
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error sending request: {ex.Message}", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            Logger.Log(ex);
        }
    }

    public async Task ComputerUseAsync()
    {
        var firstMessage = !MessagesForUi.Any();
        await ExecuteRequestWithCommonHandlingAsync(async () =>
        {
            ProgressStatus = "AI is executing actions. Please wait and avoid interaction until completion.";

            byte[] screenshot = ScreenCapturer.CaptureFocusedScreenScreenshot(out _screenBounds);

            string request = RequestDoc.Text;
            RequestDoc.Text = string.Empty;

            await AddUiMessageSegmentAsync(new() { Author = IdentifierEnum.Me, Content = request });

            cancellationTokenSource = new();

            Task<ComputerUseResponse> task;

            if (!string.IsNullOrWhiteSpace(_previousResponseId))
            {
                task = ApiHandler.GetComputerUseResponseAsync(Options, request, _screenBounds.Width, _screenBounds.Height, _previousResponseId, cancellationTokenSource.Token);
            }
            else
            {
                task = ApiHandler.GetComputerUseResponseAsync(Options, request, _screenBounds.Width, _screenBounds.Height, screenshot, cancellationTokenSource.Token);
            }

            await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));

            cancellationTokenSource.Token.ThrowIfCancellationRequested();

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
                await AddUiMessageSegmentAsync(new() { Author = IdentifierEnum.ChatGPT, Content = message.Text });
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
                        $" \"{MessagesForUi[0].Segments[0].Content}\"," +
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
                cancellationTokenSource.Token
            );
        }
    }

    private void UpdateNativeTools(ref string requestToApi, ref string requestToShow)
    {
        _apiChat.ClearTools();
        if (UseSqlTools && SqlServerConnectionSelectedIndex != -1 && SqlServerConnections.Count > SqlServerConnectionSelectedIndex)
        {
            if (Options.OneShotToolMode)
            {
                requestToApi = $"""
                                {Options.SqlServerAgentCommand}
                                <task>{requestToApi}</task>
                                """;
            }
            else if (string.IsNullOrWhiteSpace(requestToApi))
            {
                requestToApi = Options.SqlServerAgentCommand;
                requestToShow = Options.SqlServerAgentCommand;
            }
        }

        if (UseApiTools && ApiDefinitionSelectedIndex != -1 && ApiDefinitions.Count > ApiDefinitionSelectedIndex)
        {
            var apiDefinition = ApiDefinitions[ApiDefinitionSelectedIndex];
            if (Options.OneShotToolMode)
            {
                requestToApi = $"""
                                {Options.APIAgentCommand}
                                <apiName>{apiDefinition.Name}</apiName>
                                <apiDefinition>{TextFormat.MinifyText(apiDefinition.Definition, string.Empty)}</apiDefinition>
                                <task>{requestToApi}</task>
                                """;
            }
            else if (string.IsNullOrWhiteSpace(requestToApi))
            {
                requestToApi = $"""
                                {Options.APIAgentCommand}
                                <apiName>{apiDefinition.Name}</apiName>
                                <apiDefinition>{TextFormat.MinifyText(apiDefinition.Definition, string.Empty)}</apiDefinition>
                                """;
                requestToShow = Options.APIAgentCommand;
            }
        }

        var enabledTools = _toolManager.GetEnabledTools();
        foreach (var tool in enabledTools)
        {
            if (!UseSqlTools && tool.Category == "SQL" || !UseApiTools && tool.Category == "API")
            {
                continue;
            }
            _apiChat.AppendFunctionCall(new FunctionRequest
            {
                Function = new Function
                {
                    Description = tool.Description,
                    Name = tool.Name,
                    Parameters = new Parameter { Properties = tool.Parameters }
                }
            });
        }
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
            var message = string.Join(Environment.NewLine, EnumerateExceptions(ex).Select(e => e.Message));
            MessageBox.Show(message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
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
            _toolManager.CancelAllPendingTools();
        }
    }

    private static IEnumerable<Exception> EnumerateExceptions(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
            yield return cur;
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
    private async Task<(string, List<FunctionResult>)> SendRequestAsync(CancellationTokenSource cts)
    {
        Task<(string, List<FunctionResult>)> task = _apiChat.GetResponseContentAndFunctionAsync();

        await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));

        cts.Token.ThrowIfCancellationRequested();

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

        (string, List<FunctionResult>) result = await SendRequestAsync(cancellationTokenSource);

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

    private async Task SendMessageToUiAsync(MessageEntity message)
    {
        try
        {
            var author = message.Segments[0].Author;
            var content = string.Join("", message.Segments.Select(s => s.Content));
            if (author == IdentifierEnum.FunctionCall)
            {
                if (!Options.ShowToolCalls)
                {
                    return;
                }

                try
                {
                    var toolResult = JsonUtils.Deserialize<ToolResult>(content);
                    content = $"""
                               <details><summary>Tool: {toolResult.Name}</summary>

                               <p><strong>Success:</strong> {toolResult.IsSuccess}</p>
                               <p><strong>Result:</strong> {toolResult.Result.Replace("\n", "<br/>")}</p>

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
                return;
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
        catch (Exception exception)
        {
            Logger.Log(exception);
            MessageBox.Show(exception.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
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
