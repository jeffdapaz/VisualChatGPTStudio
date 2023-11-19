using System.Windows.Controls;namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo{
    /// <summary>
    /// Represents a chat item in the Turbo Chat.
    /// </summary>
    internal class ChatItem    {
        /// <summary>
        /// Gets or sets the Id of the chat.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the chat.
        /// </summary>
        public string Name { get; set; }

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
        public ucChatItem ListItem { get; set; }    }}