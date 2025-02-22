using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using JeffPires.VisualChatGPTStudio.Utils;
using JeffPires.VisualChatGPTStudio.Utils.API;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
using System.Windows.Forms;
using VisualChatGPTStudioShared.Utils;

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
                if (!ValidateAPIKey())
                {
                    return;
                }

                string command = await OptionsCommands.GetCommandAsync(CommandsType.Optimize);

                if (string.IsNullOrWhiteSpace(command))
                {
                    System.Windows.MessageBox.Show(string.Format(Constants.MESSAGE_SET_COMMAND, nameof(Optimize)), Constants.EXTENSION_NAME);

                    return;
                }

                DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

                string selectedText = docView.TextView.Selection.StreamSelectionSpan.GetText();

                if (!ValidateCodeSelected(selectedText))
                {
                    return;
                }

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 1, 2);

                CancellationTokenSource = new CancellationTokenSource();

                string result = OptionsGeneral.UseCompletion && OptionsGeneral.Service == OpenAIService.OpenAI
                    ? await ApiHandler.GetCompletionResponseAsync(OptionsGeneral, command + Constants.PROVIDE_ONLY_CODE_INSTRUCTION, selectedText, OptionsGeneral.StopSequences?.Split([','], StringSplitOptions.RemoveEmptyEntries), CancellationTokenSource.Token)
                    : await ApiHandler.GetResponseAsync(OptionsGeneral, command + Constants.PROVIDE_ONLY_CODE_INSTRUCTION, selectedText, OptionsGeneral.StopSequences?.Split([','], StringSplitOptions.RemoveEmptyEntries), CancellationTokenSource.Token);

                result = TextFormat.RemoveCodeTagsFromOpenAIResponses(result.ToString());

                await DiffView.ShowDiffViewAsync(docView.FilePath, selectedText, result);

                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 2, 2);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                if (ex is not OperationCanceledException)
                {
                    Logger.Log(ex);

                    System.Windows.Forms.MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
