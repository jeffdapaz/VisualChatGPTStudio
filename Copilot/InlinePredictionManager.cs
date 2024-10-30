using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;

namespace JeffPires.VisualChatGPTStudio.Copilot
{
    /// <summary>
    /// Manages inline predictions for code completion and suggestions.
    /// </summary>
    internal class InlinePredictionManager
    {
        private readonly IWpfTextView view;
        private readonly ConcurrentDictionary<string, string> cache = new();
        private CancellationTokenSource cancellationTokenSource;
        private bool showingAutoComplete = false;

        /// <summary>
        /// Initializes a new instance of the InlinePredictionManager class with the specified text view.
        /// </summary>
        /// <param name="view">The IWpfTextView instance to be managed by the InlinePredictionManager.</param>
        public InlinePredictionManager(IWpfTextView view)
        {
            this.view = view;
        }

        /// <summary>
        /// Handles the event when the Enter key is pressed. This method captures the current caret position,
        /// retrieves the file path, formats a system message, and gets the code up to the current position.
        /// It then sends a request to ChatGPT for a code prediction, processes the prediction, and displays
        /// the autocomplete suggestion in the editor.
        /// </summary>
        public async Task OnEnterPressed(OptionPageGridGeneral options)
        {
            try
            {
                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_COPILOT, 1, 2);

                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new();

                CleanCache();

                int caretPosition = view.Caret.Position.BufferPosition.Position;

                string filePath = ((ITextDocument)view.TextDataModel.DocumentBuffer.Properties.GetProperty(typeof(ITextDocument))).FilePath;

                string systemMessage = TextFormat.FormatForCompleteCommand(options.CopilotCommand + Constants.PROVIDE_ONLY_CODE_INSTRUCTION, filePath);

                string code = GetCodeUpToCurrentPosition(caretPosition);

                string codeNormalized = TextFormat.NormalizeLineBreaks(code);
                codeNormalized = TextFormat.RemoveBlankLinesFromResult(codeNormalized).Trim();

                string cacheKey = $"{filePath}:{codeNormalized}";

                if (cache.TryGetValue(cacheKey, out string cachedPrediction))
                {
                    await Suggestions.ShowAutocompleteAsync(view, cachedPrediction, caretPosition);

                    return;
                }

                string prediction = string.IsNullOrEmpty(options.CompletionCustomModel)
                    ? await ChatGPT.GetResponseAsync(options, systemMessage, code, null, cancellationTokenSource.Token)
                    : await ChatGPT.GetCompletionResponseAsync(options, systemMessage, code, null, cancellationTokenSource.Token, options.CompletionCustomModel);

                if (cancellationTokenSource.Token.IsCancellationRequested || string.IsNullOrWhiteSpace(prediction))
                {
                    return;
                }

                prediction = FormatPrediction(code, prediction);

                cache[cacheKey] = prediction;

                await Suggestions.ShowAutocompleteAsync(view, prediction, caretPosition);

                showingAutoComplete = true;
            }
            catch (OperationCanceledException)
            {
                showingAutoComplete = false;
            }
            catch (Exception ex)
            {
                showingAutoComplete = false;
                Logger.Log(ex);
            }
            finally
            {
                await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_COPILOT, 2, 2);
            }
        }

        /// <summary>
        /// Handles the Tab key press event asynchronously, switching to the main thread and executing a document edit command.
        /// </summary>
        public async Task OnTabPressedAsync()
        {
            try
            {
                if (!showingAutoComplete)
                {
                    return;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                (await VS.GetServiceAsync<DTE, DTE>()).ExecuteCommand(Constants.EDIT_DOCUMENT_COMMAND);
            }
            catch (Exception)
            {

            }
            finally
            {
                showingAutoComplete = false;
            }
        }

        /// <summary>
        /// Retrieves the code from the current caret position up to the start of the method or the beginning of the document.
        /// </summary>
        /// <param name="caretPosition">The current position of the caret in the text buffer.</param>
        /// <returns>
        /// A string containing the code from the current caret position up to the start of the method or the beginning of the document.
        /// </returns>
        private string GetCodeUpToCurrentPosition(int caretPosition)
        {
            ITextSnapshot snapshot = view.TextBuffer.CurrentSnapshot;
            ITextSnapshotLine currentLine = snapshot.GetLineFromPosition(caretPosition);
            StringBuilder codeUpToCurrentPosition = new();

            // Regex to identify the beginning of methods 
            Regex methodStartRegex = new(@"^\s*(public|private|protected|internal|static|\s)*\s*(void|int|string|bool|char|class|struct|[A-Za-z0-9_]+)\s+[A-Za-z0-9_]+\s*\(.*\)\s*\{?", RegexOptions.Compiled);

            while (currentLine.LineNumber >= 0)
            {
                string lineText = currentLine.GetText().Trim();

                codeUpToCurrentPosition.Insert(0, lineText + Environment.NewLine);

                if (methodStartRegex.IsMatch(lineText))
                {
                    break;
                }

                if (currentLine.LineNumber == 0)
                {
                    break;
                }

                currentLine = snapshot.GetLineFromLineNumber(currentLine.LineNumber - 1);
            }

            return codeUpToCurrentPosition.ToString();
        }

        /// <summary>
        /// Removes the original code from the prediction while maintaining formatting and line breaks.
        /// </summary>
        /// <param name="originalCode">The original code to be removed.</param>
        /// <param name="prediction">The prediction from which the original code will be removed.</param>
        /// <returns>The cleaned prediction without the original code.</returns>
        private string FormatPrediction(string originalCode, string prediction)
        {
            originalCode = TextFormat.NormalizeLineBreaks(originalCode);
            originalCode = TextFormat.RemoveBlankLinesFromResult(originalCode).Trim();

            prediction = TextFormat.RemoveCodeTagsFromOpenAIResponses(prediction).Trim();

            List<string> originalLines = originalCode.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            List<string> predictionLines = prediction.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            // Remove the original code block from the beginning of the prediction
            int j = 0;

            while (j < originalLines.Count && j < predictionLines.Count && originalLines[j].Trim() == predictionLines[j].Trim())
            {
                originalLines.RemoveAt(j);
                predictionLines.RemoveAt(j);
            }

            // Remove any remaining lines that match the original code
            for (int i = 0; i < predictionLines.Count && i < originalLines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(originalLines[i]))
                {
                    continue;
                }

                predictionLines[i] = predictionLines[i].Replace(originalLines[i].Trim(), string.Empty);
            }

            return string.Join(Environment.NewLine, predictionLines);
        }

        /// <summary>
        /// Cleans the cache by removing the oldest entries if the cache size exceeds the maximum limit.
        /// The method ensures that the cache size is reduced to half of the maximum allowed size.
        /// </summary>
        private void CleanCache()
        {
            const int MAX_CACHE_SIZE = 10;

            if (cache.Count > MAX_CACHE_SIZE)
            {
                List<string> keysToRemove = cache.Keys.Take(cache.Count - MAX_CACHE_SIZE / 2).ToList();

                foreach (string key in keysToRemove)
                {
                    cache.TryRemove(key, out _);
                }
            }
        }
    }
}