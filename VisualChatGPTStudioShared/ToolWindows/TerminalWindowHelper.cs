using System;
using System.Windows;
using System.Windows.Automation;
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
        /// Copies the given text to the clipboard and updates the given image to show a checkmark and tooltip indicating the text has been copied. After 2 seconds, the image is updated back to its original state.
        /// </summary>
        /// <param name="image">The image to update.</param>
        /// <param name="text">The text to copy to the clipboard.</param>
        public static void Copy(Image image, string text)
        {
            Clipboard.SetText(text);

            image.Source = new BitmapImage(new Uri("pack://application:,,,/VisualChatGPTStudio;component/Resources/check.png"));
            image.ToolTip = "Copied!";
            image.IsEnabled = false;

            AutomationProperties.SetHelpText(image, "Copied!");

            System.Timers.Timer timer = new(2000) { Enabled = true };

            timer.Elapsed += (s, args) =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/VisualChatGPTStudio;component/Resources/copy.png"));
                    image.ToolTip = "Copy code";
                    image.IsEnabled = true;

                    AutomationProperties.SetHelpText(image, "Copy code");
                }));

                timer.Enabled = false;
                timer.Dispose();
            };
        }
    }
}
