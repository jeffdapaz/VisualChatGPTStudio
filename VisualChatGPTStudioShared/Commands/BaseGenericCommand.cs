using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using Span = Microsoft.VisualStudio.Text.Span;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    /// <summary>
    /// Base abstract class for generic commands
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <seealso cref="BaseCommand&lt;&gt;" />
    internal abstract class BaseGenericCommand<TCommand> : BaseCommand<TCommand> where TCommand : class, new()
    {
        protected DocumentView docView;
        private string selectedText;
        private int position;
        private int positionStart;
        private int positionEnd;
        private bool firstIteration;
        private bool responseStarted;

        /// <summary>
        /// Gets the type of command.
        /// </summary>
        /// <param name="selectedText">The selected text.</param>
        /// <returns>The type of command.</returns>
        protected abstract CommandType GetCommandType(string selectedText);

        /// <summary>
        /// Gets the command for the given selected text.
        /// </summary>
        /// <param name="selectedText">The selected text.</param>
        /// <returns>The command for the given selected text.</returns>
        protected abstract string GetCommand(string selectedText);

        /// <summary>
        /// Executes asynchronously when the command is invoked and <see cref="M:Community.VisualStudio.Toolkit.BaseCommand.Execute(System.Object,System.EventArgs)" /> isn't overridden.
        /// </summary>
        protected override async System.Threading.Tasks.Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (!ValidateAPIKey())
                {
                    return;
                }

                firstIteration = true;
                responseStarted = false;

                await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

                docView = await VS.Documents.GetActiveDocumentViewAsync();

                if (docView?.TextView == null)
                {
                    return;
                }

                position = docView.TextView.Caret.Position.BufferPosition.Position;
                positionStart = docView.TextView.Selection.Start.Position.Position;
                positionEnd = docView.TextView.Selection.End.Position.Position;

                selectedText = docView.TextView.Selection.StreamSelectionSpan.GetText();

                if (!ValidateCodeSelected(selectedText))
                {
                    return;
                }

                await RequestAsync(selectedText);
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

        /// <summary>
        /// Requests a response from ChatGPT and handles the result.
        /// </summary>
        /// <param name="selectedText">The selected text.</param>
        private async System.Threading.Tasks.Task RequestAsync(string selectedText)
        {
            string command = GetCommand(selectedText);

            if (typeof(TCommand) != typeof(AskAnything) && string.IsNullOrWhiteSpace(command))
            {
                System.Windows.Forms.MessageBox.Show(string.Format(Constants.MESSAGE_SET_COMMAND, typeof(TCommand).Name), Constants.EXTENSION_NAME);

                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                await TerminalWindowCommand.Instance.RequestToWindowAsync(command, selectedText, IsCodeCommand());

                return;
            }

            await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 1, 2);

            string[] stopSequences = OptionsGeneral.StopSequences.Split([','], StringSplitOptions.RemoveEmptyEntries);

            if (typeof(TCommand) == typeof(AddSummary))
            {
                stopSequences = new[] { "public", "private", "internal" };
            }

            CancellationTokenSource = new CancellationTokenSource();

            string result;

            if (OptionsGeneral.UseCompletion && OptionsGeneral.Service == OpenAIService.OpenAI)
            {
                result = await ChatGPT.GetCompletionResponseAsync(OptionsGeneral, command, selectedText, stopSequences, CancellationTokenSource.Token);

                ResultHandler(result);
            }
            else
            {
                if (OptionsGeneral.SingleResponse || IsCodeCommand())
                {
                    result = await ChatGPT.GetResponseAsync(OptionsGeneral, command, selectedText, stopSequences, CancellationTokenSource.Token);

                    ResultHandler(result);
                }
                else
                {
                    await ChatGPT.GetResponseAsync(OptionsGeneral, command, selectedText, stopSequences, ResultHandler, CancellationTokenSource.Token);
                }
            }

            await FormatDocumentAsync();
        }

        /// <summary>
        /// Handles the result of a command sent to ChatGPT.
        /// </summary>
        /// <param name="result">The result of the command.</param>
        private void ResultHandler(string result)
        {
            try
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (firstIteration)
                {
                    _ = VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 2, 2);

                    CommandType commandType = GetCommandType(selectedText);

                    if (commandType == CommandType.Replace)
                    {
                        position = positionStart;

                        //Erase current code
                        _ = docView.TextBuffer?.Replace(new Span(position, docView.TextView.Selection.StreamSelectionSpan.GetText().Length), String.Empty);
                    }
                    else if (commandType == CommandType.InsertBefore)
                    {
                        position = positionStart;

                        InsertANewLine(false);
                    }
                    else
                    {
                        position = positionEnd;

                        InsertANewLine(true);
                    }

                    if (typeof(TCommand) == typeof(Explain) || typeof(TCommand) == typeof(FindBugs))
                    {
                        AddCommentChars();
                    }

                    firstIteration = false;
                }

                result = TextFormat.NormalizeLineBreaks(result);

                if (OptionsGeneral.SingleResponse)
                {
                    result = TextFormat.RemoveBlankLinesFromResult(result);
                }
                else if (!responseStarted && (result.Equals("\n") || result.Equals("\r") || result.Equals(Environment.NewLine)))
                {
                    //Do nothing when API send only break lines on response begin
                    return;
                }

                responseStarted = true;

                if (typeof(TCommand) == typeof(Explain) || typeof(TCommand) == typeof(FindBugs))
                {
                    result = FormatResultToAddCommentsCharForEachLine(result);
                }
                else if (IsCodeCommand())
                {
                    result = TextFormat.RemoveCodeTagsFromOpenAIResponses(result);
                }

                docView.TextBuffer?.Insert(position, result);

                position += result.Length;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        /// <summary>
        /// Inserts a new line into the document at the given position and optionally moves the position to the start of the next line.
        /// </summary>
        /// <param name="moveToNextLine">Indicates whether the position should be moved to the start of the next line.</param>
        private void InsertANewLine(bool moveToNextLine)
        {
            ITextSnapshot textSnapshot = docView.TextBuffer?.Insert(position, Environment.NewLine);

            // Get the next line
            ITextSnapshotLine nextLine = textSnapshot.GetLineFromLineNumber(textSnapshot.GetLineNumberFromPosition(position) + 1);

            if (moveToNextLine)
            {
                // Get the position of the first character on the next line
                position = nextLine.Start.Position;
            }
        }

        /// <summary>
        /// This method adds comment characters to the text buffer at the specified position.
        /// </summary>
        private void AddCommentChars()
        {
            string commentChars = TextFormat.GetCommentChars(docView.FilePath);

            docView.TextBuffer?.Insert(position, commentChars);
            position += commentChars.Length;
        }

        /// <summary>
        /// Formats the result to add comments char for each line.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>
        /// The formatted result.
        /// </returns>
        private string FormatResultToAddCommentsCharForEachLine(string result)
        {
            string commentChars = TextFormat.GetCommentChars(docView.FilePath);

            string[] lines = TextFormat.SplitTextByLine(result);

            result = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    result += lines[i];

                    if (i < lines.Length - 1)
                    {
                        result += $"{Environment.NewLine}{commentChars}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if the generic type TCommand is one of the specified command types.
        /// </summary>
        private bool IsCodeCommand()
        {
            return typeof(TCommand) == typeof(AddSummary) || typeof(TCommand) == typeof(AddTests) || typeof(TCommand) == typeof(Complete) || typeof(TCommand) == typeof(Optimize);
        }
    }

    /// <summary>
    /// Enum to represent the different types of commands that can be used.
    /// </summary>
    enum CommandType
    {
        Replace,
        InsertBefore,
        InsertAfter
    }
}