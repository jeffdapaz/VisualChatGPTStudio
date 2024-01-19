using ICSharpCode.AvalonEdit.Document;
using JeffPires.VisualChatGPTStudio.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    /// <summary>
    /// Represents an item on the ucChat ListControl.
    /// </summary>
    public class ChatListControlItem
    {
        public string ImageSource { get; private set; }

        public string Text { get; set; }

        public TextDocument Document { get; private set; }

        public Visibility TextVisibility { get; private set; }

        public Visibility CodeVisibility { get; private set; }

        public string Syntax { get; private set; }

        public Brush BackgroundColor { get; private set; }

        public Thickness Margins { get; private set; }

        public Visibility ButtonCopyVisibility { get; private set; }

        public int Index { get; set; }

        public ScrollBarVisibility ShowHorizontalScrollBar { get; set; }

        public CornerRadius CornerRadius { get; set; }

        /// <summary>
        /// Constructor for ChatTurboItem class.
        /// </summary>
        /// <param name="author">Author of the message.</param>
        /// <param name="message">Message content.</param>
        /// <param name="firstSegment">Indicates if the message is the first segment of the conversation.</param>
        /// <param name="lastSegment">Indicates if the message is the last segment of the conversation.</param>
        /// <param name="index">Index of the message.</param>
        public ChatListControlItem(AuthorEnum author, string message, bool firstSegment, bool lastSegment, int index)
        {
            if (author == AuthorEnum.Me)
            {
                Text = message;
                ImageSource = "pack://application:,,,/VisualChatGPTStudio;component/Resources/vs.png";
                BackgroundColor = new SolidColorBrush(Color.FromRgb(153, 187, 255));
                Syntax = string.Empty;
                Margins = index == 0 ? new Thickness(0, 0, 0, 25) : new Thickness(0, 25, 0, 25);
                ButtonCopyVisibility = Visibility.Collapsed;
                ShowHorizontalScrollBar = ScrollBarVisibility.Disabled;
                TextVisibility = Visibility.Visible;
                CodeVisibility = Visibility.Collapsed;
                CornerRadius = new CornerRadius(5);
            }
            else if (author == AuthorEnum.ChatGPT)
            {
                Text = message;
                ImageSource = firstSegment ? "pack://application:,,,/VisualChatGPTStudio;component/Resources/chatGPT.png" : string.Empty;
                BackgroundColor = new SolidColorBrush(Color.FromRgb(194, 194, 214));
                Syntax = string.Empty;
                Margins = lastSegment ? new Thickness(0, -2.5, 0, 0) : new Thickness(0, -2.5, 0, -2.5);
                ButtonCopyVisibility = Visibility.Collapsed;
                ShowHorizontalScrollBar = ScrollBarVisibility.Disabled;
                TextVisibility = Visibility.Visible;
                CodeVisibility = Visibility.Collapsed;

                if (firstSegment && lastSegment)
                {
                    CornerRadius = new CornerRadius(5, 5, 5, 5);
                }
                else if (!firstSegment && !lastSegment)
                {
                    CornerRadius = new CornerRadius(0, 0, 0, 0);
                }
                else if (firstSegment)
                {
                    CornerRadius = new CornerRadius(5, 5, 0, 0);
                }
                else if (lastSegment)
                {
                    CornerRadius = new CornerRadius(0, 0, 5, 5);
                }
            }
            else if (author == AuthorEnum.ChatGPTCode)
            {
                Document = new TextDocument(message);
                ImageSource = firstSegment ? "pack://application:,,,/VisualChatGPTStudio;component/Resources/chatGPT.png" : string.Empty;
                BackgroundColor = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                Syntax = TextFormat.DetectCodeLanguage(message);
                Margins = new Thickness(0, -2.5, 0, -2.5);
                ButtonCopyVisibility = Visibility.Visible;
                ShowHorizontalScrollBar = ScrollBarVisibility.Auto;
                TextVisibility = Visibility.Collapsed;
                CodeVisibility = Visibility.Visible;
                CornerRadius = new CornerRadius(0);
            }

            Index = index;
        }
    }

    public enum AuthorEnum
    {
        Me,
        ChatGPT,
        ChatGPTCode
    }
}
