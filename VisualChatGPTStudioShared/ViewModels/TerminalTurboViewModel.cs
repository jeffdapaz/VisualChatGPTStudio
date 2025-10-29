using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;
using JeffPires.VisualChatGPTStudio.Utils.Repositories;

namespace VisualChatGPTStudioShared.ToolWindows.Turbo
{
    public sealed class TerminalTurboViewModel : INotifyPropertyChanged
    {
        private string search = string.Empty;
        private List<ChatEntity> filtered = [];
        private int page = 0;
        private const int PageSize = 10;

        public TerminalTurboViewModel()
        {
            ReloadChats();
            var lastChat = AllChats.LastOrDefault();
            if (lastChat != null)
            {
                LoadChat(lastChat.Id);
            }

            ApplyFilter();
        }

        public List<string> SqlServerConnectionsAlreadyAdded { get; private set; } = [];

        public List<string> ApiDefinitionsAlreadyAdded { get; private set; } = [];

        /// <summary>
        /// Chat list from Database
        /// </summary>
        private List<ChatEntity> AllChats { get; set; } = [];

        /// <summary>
        /// Current ChatId
        /// </summary>
        public string ChatId { get; private set; }

        public List<MessageEntity> Messages { get; set; }

        public ObservableCollection<ChatEntity> Chats { get; } = [];

        public int PageNumber => page + 1;

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((filtered?.Count ?? 0.0) / PageSize));

        public string CurrentPageView => $"{PageNumber} / {TotalPages}";

        public bool CanGoPrev => page > 0;

        public bool CanGoNext => page < TotalPages - 1;

        public ICommand NextCmd => new RelayCommand(() =>
            {
                page++;
                ApplyFilter();
            },
            () => CanGoNext);

        public ICommand PrevCmd => new RelayCommand(() =>
            {
                page--;
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
            }
        }

        public string Search
        {
            get => search;
            set
            {
                if (SetField(ref search, value))
                    ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            if (!string.IsNullOrEmpty(Search))
            {
                filtered = AllChats
                    .Where(c => c.Name.ToLower().Contains(Search.ToLower()))
                    .ToList();
            }
            else
            {
                filtered = AllChats;
            }

            if (page >= TotalPages)
            {
                page = TotalPages - 1;
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
            var itemsOnPage = filtered;
            if (itemsOnPage.Count > PageSize)
            {
                var itemsToSkip = page * PageSize;
                itemsOnPage = filtered.Skip(itemsToSkip).Take(PageSize).ToList();
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

        private void ReloadChats()
        {
            AllChats = ChatRepository.GetChats();
            foreach (var c in AllChats)
            {
                c.IsSelected = ChatId == c.Id;
            }
        }

        public MessageEntity AddMessageSegment(ChatMessageSegment segment)
        {
            if (string.IsNullOrEmpty(ChatId))
            {
                CreateNewChat();
            }
            var mes = new MessageEntity { Order = Messages.Count + 1, Segments = [segment] };
            Messages.Add(mes);
            return mes;
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
            ForceReloadChats();
            LoadChat();
        }

        public void LoadChat(string id = "")
        {
            var selectedChat = string.IsNullOrEmpty(id)
                ? AllChats.FirstOrDefault()
                : AllChats.FirstOrDefault(c => c.Id == id);

            ChatId = selectedChat?.Id ?? string.Empty;
            Messages = !string.IsNullOrEmpty(ChatId) ? ChatRepository.GetMessages(ChatId) : [];
            SqlServerConnectionsAlreadyAdded = ChatRepository.GetSqlServerConnections(ChatId);
            ApiDefinitionsAlreadyAdded = ChatRepository.GetApiDefinitions(ChatId);
        }

        public void ForceReloadChats()
        {
            ReloadChats();
            ApplyFilter();
        }

        // TODO: toolbar buttons visibility
        public bool IsProcessing { get; set; } = false;

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
