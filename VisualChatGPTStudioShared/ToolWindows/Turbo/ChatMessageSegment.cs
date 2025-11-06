namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    /// <summary>
    /// This class represents a chat message segment.
    /// </summary>
    public class ChatMessageSegment
    {
        /// <summary>
        /// Segment author.
        /// </summary>
        public IdentifierEnum Author { get; set; }

        /// <summary>
        /// The message content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// To Control the segment position by the start.
        /// </summary>
        public int SegmentOrderStart { get; set; }

        /// <summary>
        /// To Control the segment position by the end.
        /// </summary>
        public int SegmentOrderEnd { get; set; }
    }

    public enum IdentifierEnum
    {
        Me,
        ChatGPT,
        ChatGPTCode,
        FunctionCall,
        FunctionRequest,
        Table,
    }
}
