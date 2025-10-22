using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

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

        /// <summary>
        /// Applies the specified code to the currently active document in Visual Studio.
        /// </summary>
        /// <param name="code">The code to insert or replace in the active document.</param>
        public static async System.Threading.Tasks.Task ApplyCodeToActiveDocumentAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return;
                }

                DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

                if (docView == null)
                {
                    System.Windows.MessageBox.Show("No active document is open to apply the code.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                    return;
                }

                await docView.TextBuffer.Properties.GetOrCreateSingletonProperty<TaskScheduler>(() => TaskScheduler.FromCurrentSynchronizationContext());

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                ITextBuffer textBuffer = docView.TextView.TextBuffer;
                NormalizedSnapshotSpanCollection selection = docView.TextView.Selection.SelectedSpans;

                using (ITextEdit edit = textBuffer.CreateEdit())
                {
                    if (selection.Count > 0 && !selection[0].IsEmpty)
                    {
                        edit.Replace(selection[0], code);
                    }
                    else
                    {
                        int caretPosition = docView.TextView.Caret.Position.BufferPosition.Position;

                        edit.Insert(caretPosition, code);
                    }

                    edit.Apply();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                System.Windows.MessageBox.Show("Failed to apply the code to the active document.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
