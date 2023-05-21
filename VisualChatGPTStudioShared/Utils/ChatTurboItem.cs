using ICSharpCode.AvalonEdit.Document;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using System.Windows.Interop;
using System;

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

        public Thickness Margins { get; private set; }

        public Visibility ButtonCopyVisibility { get; private set; }

        public int Index { get; set; }

        public ScrollBarVisibility ShowHorizontalScrollBar { get; set; }

        /// <summary>
        /// Constructor for ChatTurboItem class.
        /// </summary>
        /// <param name="author">Author of the message.</param>
        /// <param name="message">Message content.</param>
        /// <param name="firstSegment">Indicates if the message is the first segment of the conversation.</param>
        /// <param name="index">Index of the message.</param>
        public ChatTurboItem(AuthorEnum author, string message, bool firstSegment, int index)
        {
            Document = new TextDocument(message);

            if (author == AuthorEnum.Me)
            {
                ImageSource = "pack://application:,,,/VisualChatGPTStudio;component/Resources/vs.png";
                BackgroundColor = new SolidColorBrush(Color.FromRgb(153, 187, 255));
                Syntax = string.Empty;
                Margins = new Thickness(0, 5, 0, 5);
                ButtonCopyVisibility = Visibility.Collapsed;
                ShowHorizontalScrollBar = ScrollBarVisibility.Disabled;
            }
            else if (author == AuthorEnum.ChatGPT)
            {
                ImageSource = firstSegment ? "pack://application:,,,/VisualChatGPTStudio;component/Resources/chatGPT.png" : string.Empty;
                BackgroundColor = new SolidColorBrush(Color.FromRgb(194, 194, 214));
                Syntax = string.Empty;
                Margins = new Thickness(0, -2.5, 0, -2.5);
                ButtonCopyVisibility = Visibility.Collapsed;
                ShowHorizontalScrollBar = ScrollBarVisibility.Disabled;
            }
            else if (author == AuthorEnum.ChatGPTCode)
            {
                ImageSource = firstSegment ? "pack://application:,,,/VisualChatGPTStudio;component/Resources/chatGPT.png" : string.Empty;
                BackgroundColor = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                Syntax = TextFormat.DetectCodeLanguage(message);
                Margins = new Thickness(0, -2.5, 0, -2.5);
                ButtonCopyVisibility = Visibility.Visible;
                ShowHorizontalScrollBar = ScrollBarVisibility.Auto;
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
