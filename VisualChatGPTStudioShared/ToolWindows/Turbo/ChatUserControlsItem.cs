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
        /// Gets or sets the chat header user control.
        /// </summary>
        public ucChatHeader Header { get; set; }

        /// <summary>
        /// Gets or sets the TabItem associated with this chat.
        /// </summary>
        public TabItem TabItem { get; set; }

        /// <summary>
        /// Gets or sets the ucChatItem object representing a chat item in the chat list.
        /// </summary>
        public ucChatItem ListItem { get; set; }        /// <summary>
        /// Indicates if this chat item was opened before
        /// </summary>        public bool OpenedBefore { get; set; }    }}