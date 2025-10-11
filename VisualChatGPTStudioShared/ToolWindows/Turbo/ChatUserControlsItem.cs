using System.Windows.Controls;using VisualChatGPTStudioShared.ToolWindows.Turbo;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo{
    /// <summary>
    /// This class keep the items and controls chat references
    /// </summary>
    internal class ChatUserControlsItem    {
        /// <summary>
        /// The Chat Entity.
        /// </summary>
        public ChatEntity Chat { get; set; }

        /// <summary>
        /// Gets or sets the TabItem associated with this chat.
        /// </summary>
        public TabItem TabItem { get; set; }
        /// <summary>
        /// Indicates if this chat item was opened before
        /// </summary>        public bool OpenedBefore { get; set; }    }}
