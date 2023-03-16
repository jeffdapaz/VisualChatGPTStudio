using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.VisualStudio.Shell;
using OpenAI_API.Completions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Span = Microsoft.VisualStudio.Text.Span;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    /// <summary>
    /// Base abstract class for commands
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <seealso cref="BaseCommand&lt;&gt;" />
    internal abstract class BaseChatGPTCommand<TCommand> : BaseCommand<TCommand> where TCommand : class, new()
    {
        const string EXTENSION_NAME = "Visual chatGPT Studio";

        protected DocumentView docView;
        private string selectedText;
        private int position;
        private int positionStart;
        private int positionEnd;
        private int lineLength;
        private bool firstInteration;

        /// <summary>
        /// Gets the OptionsGeneral property of the VisuallChatGPTStudioPackage.
        /// </summary>
        protected OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).OptionsGeneral;
            }
        }

        /// <summary>
        /// Gets the OptionsCommands property of the VisuallChatGPTStudioPackage.
        /// </summary>
        protected OptionPageGridCommands OptionsCommands
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).OptionsCommands;
            }
        }

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
        /// <returns>The command.</returns>
        protected abstract string GetCommand(string selectedText);

        /// <summary>
        /// Executes asynchronously when the command is invoked and <see cref="M:Community.VisualStudio.Toolkit.BaseCommand.Execute(System.Object,System.EventArgs)" /> isn't overridden.
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// Use this method instead of <see cref="M:Community.VisualStudio.Toolkit.BaseCommand.Execute(System.Object,System.EventArgs)" /> if you're invoking any async tasks by using async/await patterns.
        /// </remarks>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(OptionsGeneral.ApiKey))
                {
                    await VS.MessageBox.ShowAsync(EXTENSION_NAME, "Please, set the OpenAI API key.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    Package.ShowOptionPage(typeof(OptionPageGridGeneral));

                    return;
                }

                firstInteration = true;
                lineLength = 0;

                await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

                docView = await VS.Documents.GetActiveDocumentViewAsync();

                if (docView?.TextView == null) return;

                position = docView.TextView.Caret.Position.BufferPosition.Position;
                positionStart = docView.TextView.Selection.Start.Position.Position;
                positionEnd = docView.TextView.Selection.End.Position.Position;

                selectedText = docView.TextView.Selection.StreamSelectionSpan.GetText();

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    await VS.MessageBox.ShowAsync(EXTENSION_NAME, "Please select the code.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                if (CheckIfSelectedTwoOrMoreMethods(selectedText))
                {
                    await VS.MessageBox.ShowAsync(EXTENSION_NAME, "Please select one method at a time.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                await RequestAsync(selectedText);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                await VS.MessageBox.ShowAsync(EXTENSION_NAME, ex.Message, Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
            }
        }

        /// <summary>
        /// Requests the specified selected text from ChatGPT
        /// </summary>
        /// <param name="selectedText">The selected text.</param>
        private async Task RequestAsync(string selectedText)
        {
            string command = GetCommand(selectedText);

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                _ = TerminalWindowCommand.Instance.RequestToWindowAsync(command);

                return;
            }

            await VS.StatusBar.ShowProgressAsync("Waiting chatGPT response", 1, 2);

            await ChatGPT.RequestAsync(OptionsGeneral, command, ResultHandler);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                //Some documents does not has format
                (await VS.GetServiceAsync<DTE, DTE>()).ExecuteCommand("Edit.FormatDocument", string.Empty);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Results handler.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="result">The result.</param>
        private async void ResultHandler(int index, CompletionResult result)
        {
            try
            {
                if (firstInteration)
                {
                    await VS.StatusBar.ShowProgressAsync("Receiving chatGPT response", 2, 2);

                    CommandType commandType = GetCommandType(selectedText);

                    if (commandType == CommandType.Erase)
                    {
                        position = positionStart;

                        //Erase current code
                        _ = (docView.TextBuffer?.Replace(new Span(position, docView.TextView.Selection.StreamSelectionSpan.GetText().Length), String.Empty));
                    }
                    else if (commandType == CommandType.InsertBefore)
                    {
                        position = positionStart;

                        _ = (docView.TextBuffer?.Insert(position, Environment.NewLine));
                    }
                    else
                    {
                        position = positionEnd;

                        _ = (docView.TextBuffer?.Insert(position, Environment.NewLine));
                    }

                    firstInteration = false;
                }

                string resultText = result.ToString();

                docView.TextBuffer?.Insert(position, resultText);

                position += resultText.Length;

                lineLength += resultText.Length;

                if (lineLength > 160 && typeof(TCommand) == typeof(AskAnything))
                {
                    lineLength = 0;
                    MovetoNextLine();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Move to the next line.
        /// </summary>
        private void MovetoNextLine()
        {
            position = docView.TextView.Caret.Position.BufferPosition.GetContainingLine().End.Position;

            docView.TextView.Caret.MoveTo(docView.TextView.Caret.Position.BufferPosition.GetContainingLine().End);

            _ = (docView.TextBuffer?.Insert(position, Environment.NewLine));
        }

        /// <summary>
        /// Check If Selected Two Or More Methods
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns>True if has Selected Two Or More Methods</returns>
        private bool CheckIfSelectedTwoOrMoreMethods(string text)
        {
            string[] words = text.Split(' ');

            return words.Count(w => w == "public" || w == "private" || w == "protected") >= 2;
        }
    }

    /// <summary>
    /// Enum to represent the different types of commands that can be used. 
    /// </summary>
    enum CommandType
    {
        Erase,
        InsertBefore,
        InsertAfter
    }
}
