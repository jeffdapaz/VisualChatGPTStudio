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
        public AuthorEnum Author { get; set; }

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
}
