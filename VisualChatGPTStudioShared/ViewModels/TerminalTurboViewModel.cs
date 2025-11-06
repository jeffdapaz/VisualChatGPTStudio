using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Agents;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;
using JeffPires.VisualChatGPTStudio.Utils;
using JeffPires.VisualChatGPTStudio.Utils.Repositories;
using OpenAI_API.Chat;
using OpenAI_API.Functions;
using VisualChatGPTStudioShared.Agents.ApiAgent;

namespace VisualChatGPTStudioShared.ToolWindows.Turbo
{
    public sealed class TerminalTurboViewModel : INotifyPropertyChanged
    {
        private string _search = string.Empty;
        private List<ChatEntity> _filtered = [];
        private int _page;
        private const int PageSize = 10;
        private List<SqlServerConnectionInfo> _sqlServerConnections = [];
        private List<ApiItem> _apiDefinitions = [];
        public OptionPageGridGeneral options;
        public Conversation apiChat;

        /// <summary>
        /// Chat list from Database
        /// </summary>
        private List<ChatEntity> AllChats { get; set; } = [];

        public int ToolCallAttempt = 0;

        public int ToolCallMaxAttempts = 10;

        public List<SqlServerConnectionInfo> SqlServerConnections
        {
            get => _sqlServerConnections;
            set => SetField(ref _sqlServerConnections, value);
        }

        public List<ApiItem> ApiDefinitions
        {
            get => _apiDefinitions;
            set => SetField(ref _apiDefinitions, value);
        }

        public byte[] AttachedImage;

        /// <summary>
        /// Current ChatId
        /// </summary>
        public string ChatId { get; private set; }

        public ObservableCollection<MessageEntity> Messages { get; } = [];

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

        public ICommand RenameChatCmd =>
            new RelayCommand<ChatEntity>(chat =>
            {
                if (chat?.Name == null || chat.EditName == null)
                {
                    return;
                }

                if (!string.Equals(chat.Name, chat.EditName, StringComparison.Ordinal))
                {
                    chat.Name = chat.EditName;
                    ChatRepository.UpdateChatName(chat.Id, chat.Name);
                }
                chat.IsEditing = false;
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

                ApiDefinitions = [];
                SqlServerConnections = [];
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

        public void AddMessageSegment(ChatMessageSegment segment)
        {
            if (string.IsNullOrEmpty(segment.Content))
            {
                return;
            }
            if (string.IsNullOrEmpty(ChatId))
            {
                CreateNewChat();
            }
            Messages.Add(new MessageEntity { Order = Messages.Count + 1, Segments = [ segment ] });
        }

        public void UpdateChatHeader(string header)
        {
            ChatRepository.UpdateChatName(ChatId, header);
            var entity = Chats.FirstOrDefault(c => c.Id == ChatId);
            if (entity != null)
            {
                entity.Name = header;
                entity.EditName = header;
            }
        }

        public void CreateNewChat()
        {
            ChatRepository.AddChat(new ChatEntity
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTime.Now,
                Messages = [],
                Name = $"New chat {AllChats.Count + 1}"
            });
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

            apiChat = ApiHandler.CreateConversation(options, options.TurboChatBehavior);
            foreach (var messageEntity in Messages)
            {
                var segment = messageEntity.Segments.FirstOrDefault();
                if (segment == null || segment.Author == IdentifierEnum.Api)
                {
                    continue;
                }
                apiChat.Messages.Add(new ChatMessage
                    {
                        Role = segment.Author switch
                        {
                            IdentifierEnum.Me => ChatMessageRole.User,
                            IdentifierEnum.ChatGPT or IdentifierEnum.ChatGPTCode => ChatMessageRole.Assistant,
                            IdentifierEnum.FunctionRequest or IdentifierEnum.FunctionCall => ChatMessageRole.Tool,
                            _ => ChatMessageRole.System
                        },
                        Content = segment.Content
                    }
                );
            }
        }

        private JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = true,
            MaxDepth = 10,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

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
}
