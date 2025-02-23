using JeffPires.VisualChatGPTStudio.Utils;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    /// <summary>
    /// Represents an item on the ucChat ListControl.
    /// </summary>
    public class ChatListControlItem
    {
        public string ImageSource { get; private set; }

        public string Text { get; set; }

        public int Index { get; set; }

        /// <summary>
        /// Constructor for ChatTurboItem class.
        /// </summary>
        /// <param name="author">Author of the message.</param>
        /// <param name="message">Message content.</param>
        public ChatListControlItem(AuthorEnum author, string message)
        {
            Text = TextFormat.AdjustCodeLanguage(message);

            if (author == AuthorEnum.Me)
            {
                ImageSource = "pack://application:,,,/VisualChatGPTStudio;component/Resources/vs.png";
            }
            else if (author == AuthorEnum.ChatGPT)
            {
                ImageSource = "pack://application:,,,/VisualChatGPTStudio;component/Resources/chatGPT.png";
            }
        }
    }

    public enum AuthorEnum
    {
        Me,
        ChatGPT,
        ChatGPTCode,
        FunctionCall,
        DataBaseSchema
    }
}
