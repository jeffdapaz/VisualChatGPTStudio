using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;using System;using System.Collections.Generic;namespace VisualChatGPTStudioShared.ToolWindows.Turbo{
    /// <summary>
    /// Represents the Turbo Chat.
    /// </summary>
    public class ChatEntity    {
        /// <summary>
        /// The Chat ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The Chat Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Chat's creation date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Chat Messages
        /// </summary>
        public List<MessageEntity> Messages { get; set; }    }

    /// <summary>
    /// Represents a Turbo Chat message.
    /// </summary>
    public class MessageEntity    {
        /// <summary>
        /// Indicates the message order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The message segments.
        /// </summary>
        public List<ChatMessageSegment> Segments { get; set; }    }}