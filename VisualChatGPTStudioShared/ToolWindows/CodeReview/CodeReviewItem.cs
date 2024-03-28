using System.ComponentModel;namespace VisualChatGPTStudioShared.ToolWindows.CodeReview{
    /// <summary>
    /// Represents an item for code review that notifies when a property changes.
    /// </summary>
    public class CodeReviewItem : INotifyPropertyChanged    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the code review details.
        /// </summary>
        public string CodeReview { get; set; }

        /// <summary>
        /// Gets or sets the original code.
        /// </summary>
        public string OriginalCode { get; set; }

        /// <summary>
        /// Gets or sets the altered code.
        /// </summary>
        public string AlteredCode { get; set; }        private bool isExpanded;

        /// <summary>
        /// Gets or sets a value indicating whether the item is expanded.
        /// </summary>
        /// <remarks>
        /// When set, if the current value differs from the provided value, it triggers a property changed notification.
        /// </remarks>
        public bool IsExpanded        {            get            {                return isExpanded;            }            set            {                if (isExpanded != value)                {                    isExpanded = value;                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));                }            }        }            }}