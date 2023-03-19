using ICSharpCode.AvalonEdit.Document;
using System.Windows.Media;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Represents a ChatTurboItem object which contains information about a chat item.
    /// </summary>
    public class ChatTurboItem
    {
        public string ImageSource { get; private set; }

        public TextDocument Document { get; private set; }

        public string Syntax { get; private set; }

        public Brush BackgroundColor { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatTurboItem"/> class.
        /// </summary>
        /// <param name="author">The author.</param>
        /// <param name="message">The message.</param>
        public ChatTurboItem(AuthorEnum author, string message)
        {
            Document = new TextDocument(message);

            //Uncomment the line below to set syntax highlighting
            //Syntax = TextFormat.DetectCodeLanguage(message);
            Syntax = string.Empty;

            if (author == AuthorEnum.Me)
            {
                ImageSource = "pack://application:,,,/VisualChatGPTStudio;component/Resources/vs.png";
                BackgroundColor = new SolidColorBrush(Color.FromRgb(194, 194, 214));
            }
            else if (author == AuthorEnum.ChatGPT)
            {
                ImageSource = "pack://application:,,,/VisualChatGPTStudio;component/Resources/chatGPT.png";
                BackgroundColor = new SolidColorBrush(Color.FromRgb(153, 187, 255));
            }
        }
    }

    public enum AuthorEnum
    {
        Me,
        ChatGPT
    }
}
