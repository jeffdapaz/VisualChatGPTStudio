using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace JeffPires.VisualChatGPTStudio.ToolWindows
{
    /// <summary>
    /// Helper class for the terminal windows
    /// </summary>
    public static class TerminalWindowHelper
    {
        /// <summary>
        /// Copies the given text to the clipboard and changes the button image and tooltip.
        /// </summary>
        /// <param name="button">The button to change.</param>
        /// <param name="text">The text to copy.</param>
        public static void Copy(Button button, string text)
        {
            Clipboard.SetText(text);

            Image img = new() { Source = new BitmapImage(new Uri("pack://application:,,,/VisualChatGPTStudio;component/Resources/check.png")) };

            button.Content = img;
            button.ToolTip = "Copied!";

            System.Timers.Timer timer = new(2000) { Enabled = true };

            timer.Elapsed += (s, args) =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    img = new() { Source = new BitmapImage(new Uri("pack://application:,,,/VisualChatGPTStudio;component/Resources/copy.png")) };

                    button.Content = img;
                    button.ToolTip = "Copy code";
                }));

                timer.Enabled = false;
                timer.Dispose();
            };
        }
    }
}
