using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;
using JeffPires.VisualChatGPTStudio.Utils.Repositories;
using OpenAI_API.Chat;

namespace VisualChatGPTStudioShared.ToolWindows.Turbo
{
    public sealed class TerminalTurboViewModel : INotifyPropertyChanged
    {
        private string _search = string.Empty;
        private List<ChatEntity> _filtered = [];
        private int _page = 0;
        private const int _pageSize = 10;

        public TerminalTurboViewModel()
        {
            ReloadChats();
            LoadChat();

            // Create first chat
            if (SelectedChat == null)
            {
                CreateNewChat();
            }
        }

        public List<ChatEntity> AllChats { get; private set; } = [];

        public ChatEntity SelectedChat { get; private set; }

        public List<MessageEntity> Messages => SelectedChat.Messages ?? [];

        public ObservableCollection<ChatEntity> Chats { get; } = [];

        public int PageNumber => _page + 1;

        public int TotalPages => (int)Math.Ceiling((double)AllChats.Count / _pageSize);

        public bool CanGoPrev => _page > 0;

        public bool CanGoNext => _page < TotalPages - 1;

        public ICommand NextCmd => new RelayCommand(() =>
            {
                _page++;
                ReloadChats();
            },
            () => CanGoNext);

        public ICommand PrevCmd => new RelayCommand(() =>
            {
                _page--;
                ReloadChats();
            },
            () => CanGoPrev);

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
                    .OrderBy(c  => c.Name)
                    .ToList();
            }
            else
            {
                _filtered = AllChats;
            }

            _page = 0;
            ReloadChats();
        }

        private void ReloadChats()
        {
            AllChats = ChatRepository.GetChats();
            Chats.Clear();
            if (_filtered != null)
            {
                foreach (var c in _filtered)
                {
                    Chats.Add(c);
                }
            }

            OnPropertyChanged(nameof(PageNumber));
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));
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
