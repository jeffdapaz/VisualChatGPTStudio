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
            LoadChat();

            // Create first chat
            if (SelectedChat == null)
            {
                CreateNewChat();
            }

            ApplyFilter();
        }

        public List<ChatEntity> AllChats { get; private set; } = [];

        public ChatEntity SelectedChat { get; private set; }

        public List<MessageEntity> Messages => SelectedChat.Messages ?? [];

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
                    .OrderBy(c  => c.Name)
                    .ToList();
            }
            else
            {
                filtered = AllChats;
            }

            if (page > TotalPages)
            {
                page = TotalPages;
            }

            Chats.Clear();
            if (filtered != null)
            {
                var itemsOnPage = filtered;
                if (itemsOnPage.Count > PageSize)
                {
                    var itemsToSkip = page * PageSize;
                    itemsOnPage = filtered.Skip(itemsToSkip).Take(PageSize).ToList();
                }

                foreach (var c in itemsOnPage)
                {
                    Chats.Add(c);
                }
            }

            OnPropertyChanged(nameof(PageNumber));
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(CurrentPageView));
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));
        }

        private void ReloadChats()
        {
            AllChats = ChatRepository.GetChats();
        }

        public void AddMessageSegment(ChatMessageSegment segment)
        {
            Messages.Add(new MessageEntity { Order = Messages.Count + 1, Segments = [ segment ]});
        }

        public void UpdateChatHeader(string header)
        {
            SelectedChat.Name = header;
            ChatRepository.UpdateChat(SelectedChat);
        }

        public void CreateNewChat()
        {
            ChatRepository.AddChat(new ChatEntity
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTime.Now,
                Messages = [],
                Name = "New chat"
            });
            AllChats = ChatRepository.GetChats();
            ReloadChats();
            ApplyFilter();
            SelectedChat = AllChats.LastOrDefault();
        }

        public void LoadChat(string id = "")
        {
            SelectedChat = string.IsNullOrEmpty(id)
                ? AllChats.LastOrDefault()
                : AllChats.FirstOrDefault(c => c.Id == id);

            if (SelectedChat != null)
            {
                SelectedChat.Messages = ChatRepository.GetMessages(SelectedChat.Id);
            }
        }

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
