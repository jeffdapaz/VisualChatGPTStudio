using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;

namespace VisualChatGPTStudioShared.ToolWindows.Turbo
{
    /// <summary>
    /// Represents the Turbo Chat.
    /// </summary>
    public class ChatEntity : INotifyPropertyChanged
    {
        /// <summary>
        /// The Chat ID
        /// </summary>
        public string Id { get; init; }

        private string _name;

        /// <summary>
        /// The Chat Name
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Chat's creation date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Chat's creation date from SQLite for parsing
        /// </summary>
        public string DateRaw
        {
            get => Date.ToString("yyyy-MM-dd HH:mm:ss");
            set => Date = DateTime.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Chat Messages
        /// </summary>
        public List<MessageEntity> Messages { get; set; } = [];

        private bool _isSelected;

        /// <summary>
        /// Selected chat
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private bool _isEditing;

        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }

        private string _editName;
        public string EditName
        {
            get => _editName;
            set { _editName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnAllPropertiesChanged()
        {
            OnPropertyChanged(string.Empty);
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Represents a Turbo Chat message.
    /// </summary>
    public class MessageEntity
    {
        /// <summary>
        /// Indicates the message order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The message segments.
        /// </summary>
        public List<ChatMessageSegment> Segments { get; set; }
    }
}
