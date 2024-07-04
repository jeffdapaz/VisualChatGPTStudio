using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using LibGit2Sharp;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using VisualChatGPTStudioShared.ToolWindows.CodeReview;
using VisualChatGPTStudioShared.Utils;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace JeffPires.VisualChatGPTStudio.ToolWindows
{
    /// <summary>
    /// Represents a control for code review in a terminal window.
    /// </summary>
    public partial class TerminalWindowCodeReviewControl : UserControl
    {
        #region Properties

        private OptionPageGridGeneral options;
        private List<CodeReviewItem> CodeReviews;
        private CancellationTokenSource cancellationTokenSource;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the TerminalWindowCodeReviewControl class.
        /// This constructor ensures the MdXaml library is loaded successfully by creating an instance of MarkdownScrollViewer.
        /// </summary>
        public TerminalWindowCodeReviewControl()
        {
            //It is necessary for the MdXaml library load successfully
            MdXaml.MarkdownScrollViewer _ = new();

            this.InitializeComponent();

            Storyboard fadeInOutStoryboard = (Storyboard)FindResource("FadeInOut");
            fadeInOutStoryboard.Begin(prgBar, true);
        }

        #endregion Constructors

        #region Event Handlers

        /// <summary>
        /// Handles the click event of the code review button. It initiates the process of fetching current Git changes.
        /// </summary>
        private async void btnCodeReview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableDisableButtons(true);

                CodeReviews = new List<CodeReviewItem>();

                Patch changes = GitChanges.GetCurrentChanges();

                if (changes == null || !changes.Any(c => c.LinesAdded > 0 || c.LinesDeleted > 0))
                {
                    MessageBox.Show("No changes found.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);

                    return;
                }

                cancellationTokenSource = new CancellationTokenSource();

                prgBar.Maximum = changes.Select(c => c.LinesAdded > 0 || c.LinesDeleted > 0).Count();

                int index = 1;

                foreach (PatchEntryChanges change in changes)
                {
                    if (change.LinesAdded == 0 && change.LinesDeleted == 0)
                    {
                        continue;
                    }

                    CodeReviewItem codeReviewItem = await DoCodeReview(change);

                    CodeReviews.Add(codeReviewItem);

                    prgBar.Value = index;

                    index++;
                }

                reviewList.ItemsSource = CodeReviews;
            }
            catch (OperationCanceledException)
            {
                //Do nothing
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show("Git repository not found.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                EnableDisableButtons(false);
            }
        }

        /// <summary>
        /// Handles the click event of the cancel button. It disables the buttons and signals a cancellation request to the CancellationTokenSource.
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            EnableDisableButtons(false);

            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Handles the Click event of the btnExpandAll button. It expands all items in the CodeReviews collection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            CodeReviews.ForEach(c => c.IsExpanded = true);
        }

        /// <summary>
        /// Handles the Click event of the btnCollapseAll button. It collapses all items in the CodeReviews collection by setting their IsExpanded property to false.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The RoutedEventArgs instance containing the event data.</param>
        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            CodeReviews.ForEach(c => c.IsExpanded = false);
        }

        /// <summary>
        /// Handles the click event on the diff view button, retrieves the associated file name from the button's tag,
        /// finds the corresponding code review item, and displays the difference between the original and altered code.
        /// </summary>
        /// <param name="sender">The button that was clicked.</param>
        /// <param name="e">Event arguments.</param>
        private async void btnDiffView_Click(object sender, RoutedEventArgs e)
        {
            Image button = (Image)sender;

            string fileName = button.Tag.ToString();

            CodeReviewItem codeReviewItem = CodeReviews.First(c => c.FileName == fileName);

            await DiffView.ShowDiffViewAsync(codeReviewItem.FilePath, codeReviewItem.OriginalCode, codeReviewItem.AlteredCode);
        }

        /// <summary>
        /// Handles the RequestNavigate event of a Hyperlink control, opening the file specified by the URI and marking the event as handled.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RequestNavigateEventArgs"/> instance containing the event data.</param>
        private async void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                DTE2 dte2 = (DTE2)Marshal.GetActiveObject("VisualStudio.DTE");
                dte2.MainWindow.Activate();
                EnvDTE.Window w = dte2.ItemOperations.OpenFile(e.Uri.OriginalString, EnvDTE.Constants.vsViewKindCode);

                e.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        /// <summary>
        /// Handles the mouse wheel event on a text box to scroll up or down.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A MouseWheelEventArgs that contains the event data.</param>
        private void txtCodeReview_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                scrollViewer.LineDown();
            }
            else
            {
                scrollViewer.LineUp();
            }

            e.Handled = true;
        }

        #endregion Event Handlers

        #region Methods

        /// <summary>
        /// Initializes control with specified options and package.
        /// </summary>
        /// <param name="options">The general options for the control.</param>
        public void StartControl(OptionPageGridGeneral options)
        {
            this.options = options;
        }

        /// <summary>
        /// Enables or disables specific buttons and toggles visibility of certain UI elements based on the reviewing state.
        /// </summary>
        /// <param name="reviewing">Indicates whether the reviewing mode is active.</param>
        private async void EnableDisableButtons(bool reviewing)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            prgBar.Value = 0;

            btnCodeReview.IsEnabled = !reviewing;
            btnCancel.IsEnabled = reviewing;
            btnExpandAll.IsEnabled = !reviewing;
            btnCollapseAll.IsEnabled = !reviewing;
            grdProgress.Visibility = reviewing ? Visibility.Visible : Visibility.Collapsed;
            scrollViewer.Visibility = reviewing ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Performs a code review on a given patch entry change asynchronously, utilizing an AI-based chat service for generating the code review comments and separating the original and altered code segments.
        /// </summary>
        /// <param name="change">The patch entry changes containing the code to be reviewed.</param>
        /// <returns>A task that represents the asynchronous operation, resulting in a CodeReviewItem containing the review details.</returns>
        private async Task<CodeReviewItem> DoCodeReview(PatchEntryChanges change)
        {
            string codeReview = await ChatGPT.GetResponseAsync(options, options.CodeReviewCommand, change.Patch, options.StopSequences.Split(','), cancellationTokenSource.Token);

            GitChanges.SeparateCodeChanges(change.Patch, out string originalCode, out string alteredCode);

            CodeReviewItem result = new()
            {
                CodeReview = ReplaceCSharpCodeTag(codeReview),
                FileName = System.IO.Path.GetFileName(change.Path),
                FilePath = GetFullPathFromPartial(change.Path),
                OriginalCode = originalCode,
                AlteredCode = alteredCode
            };

            return result;
        }

        /// <summary>
        /// Replaces variant C# code block tags with a standardized tag for consistency.
        /// </summary>
        /// <param name="code">The code review content.</param>
        /// <returns>The modified code review content with standardized code block tags.</returns>
        private static string ReplaceCSharpCodeTag(string code)
        {
            return Regex.Replace(code, "```(c#|csharp)", "```c", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Retrieves the full path of a file from a given partial path by searching through the project items in the Visual Studio solution.
        /// </summary>
        /// <param name="partialPath">The partial path or file name to search for.</param>
        /// <returns>The full path of the file if found; otherwise, returns the original partial path.</returns>
        private string GetFullPathFromPartial(string partialPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE dte = VS.GetServiceAsync<DTE, DTE>().Result;

            string fullPath = string.Empty;

            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                fullPath = FindFileInProjectItems(project.ProjectItems, partialPath);

                if (!string.IsNullOrWhiteSpace(fullPath))
                {
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return partialPath;
            }

            return fullPath;
        }

        /// <summary>
        /// Searches for a file within a collection of project items that matches a specified partial path.
        /// </summary>
        /// <param name="items">The collection of project items to search through.</param>
        /// <param name="partialPath">The partial path of the file to find.</param>
        /// <returns>The full path of the found file or an empty string if the file is not found.</returns>
        private string FindFileInProjectItems(ProjectItems items, string partialPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in items)
            {
                if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                {
                    string result = FindFileInProjectItems(item.ProjectItems, partialPath);

                    if (!string.IsNullOrEmpty(result))
                    {
                        return result;
                    }
                }
                else if (item.FileCount > 0)
                {
                    try
                    {
                        for (short i = 1; i <= item.FileCount; i++)
                        {
                            string filePath = item.FileNames[i];

                            if (filePath.Replace('\\', '/').EndsWith(partialPath, StringComparison.OrdinalIgnoreCase))
                            {
                                return filePath;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }

            return string.Empty;
        }

        #endregion Methods                         
    }
}