using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    /// <summary>
    /// Command to add summary for the entire class.
    /// </summary>
    [Command(PackageIds.OptimizeDiffView)]
    internal sealed class OptimizeDiffView : BaseCommand<OptimizeDiffView>
    {
        /// <summary>
        /// Executes the ChatGPT optimization process for the selected code and shows on a diff view.
        /// </summary>
        protected override async System.Threading.Tasks.Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (!await ValidateAPIKeyAsync())
                {
                    return;
                }

                string command = await OptionsCommands.GetCommandAsync(CommandsType.Optimize);

                if (string.IsNullOrWhiteSpace(command))
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, string.Format(Constants.MESSAGE_SET_COMMAND, nameof(Optimize)), buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

                string selectedText = docView.TextView.Selection.StreamSelectionSpan.GetText();

                if (!await ValidateCodeSelectedAsync(selectedText))
                {
                    return;
                }

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 1, 2);

                CancellationTokenSource = new CancellationTokenSource();

                string result = await ChatGPT.GetResponseAsync(OptionsGeneral, command, selectedText, OptionsGeneral.StopSequences?.Split(','), CancellationTokenSource.Token);

                result = RemoveBlankLinesFromResult(result.ToString());

                await ShowDiffViewAsync(docView.FilePath, selectedText, result);

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_RECEIVING_CHATGPT, 2, 2);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                if (ex is not OperationCanceledException)
                {
                    Logger.Log(ex);

                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, ex.Message, Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                }
            }
        }

        /// <summary>
        /// Shows a diff view of two strings of code.
        /// </summary>
        /// <param name="originalCode">The original code.</param>
        /// <param name="optimizedCode">The optimized code.</param>
        private async System.Threading.Tasks.Task ShowDiffViewAsync(string filePath, string originalCode, string optimizedCode)
        {
            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.');

            string tempFolder = System.IO.Path.GetTempPath();
            string tempFilePath1 = System.IO.Path.Combine(tempFolder, $"Original.{extension}");
            string tempFilePath2 = System.IO.Path.Combine(tempFolder, $"Optimized.{extension}");

            System.IO.File.WriteAllText(tempFilePath1, originalCode);
            System.IO.File.WriteAllText(tempFilePath2, optimizedCode);

            EnvDTE.DTE dte = await GetDTEAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            dte.ExecuteCommand("Tools.DiffFiles", $"\"{tempFilePath1}\" \"{tempFilePath2}\"");
        }
    }
}
