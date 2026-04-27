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
using System.Windows.Threading;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;

namespace JeffPires.VisualChatGPTStudio.Copilot
{
    /// <summary>
    /// Manages inline predictions (ghost text) for a single <see cref="IWpfTextView"/>.
    /// Uses <see cref="GhostTextTagger"/> to render gray text adornments in the editor.
    /// </summary>
    internal class InlinePredictionManager
    {
        #region Properties

        private readonly OptionPageGridGeneral options;
        private readonly IWpfTextView view;
        private readonly ConcurrentDictionary<string, string> cache = new();
        private readonly DispatcherTimer typingTimer;

        private CancellationTokenSource cancellationTokenSource;
        private bool showingAutoComplete;
        private bool suppressNextSuggestion;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InlinePredictionManager"/>
        /// class wired to the supplied text view.
        /// </summary>
        /// <param name="options">Extension's general options.</param>
        /// <param name="view">The text view to attach to.</param>
        public InlinePredictionManager(OptionPageGridGeneral options, IWpfTextView view)
        {
            this.options = options;
            this.view = view;

            if (!options.CopilotEnabled)
            {
                return;
            }

            typingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(options.CopilotSuggestionInterval) };
            typingTimer.Tick += TypingTimer_Tick;

            this.view.TextBuffer.Changed += TextBuffer_Changed;
            this.view.Closed += OnViewClosed;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Restarts the typing debounce timer, allowing a new prediction
        /// to be requested (e.g. after an Enter keystroke).
        /// </summary>
        public void RestartTimer()
        {
            if (typingTimer == null)
            {
                return;
            }

            typingTimer.Stop();
            typingTimer.Start();
        }

        /// <summary>
        /// Signals that a suggestion was just accepted. When
        /// <see cref="OptionPageGridGeneral.CopilotNextEditSuggestions"/> is
        /// disabled, the next buffer change (caused by the accept insertion)
        /// will not trigger a new prediction request.
        /// </summary>
        public void NotifySuggestionAccepted()
        {
            if (!options.CopilotNextEditSuggestions)
            {
                suppressNextSuggestion = true;
            }
        }

        /// <summary>
        /// Sends the surrounding code to the language model and, if a prediction
        /// is returned, displays it as inline ghost text in the editor.
        /// </summary>
        public async Task ShowAutocompleteAsync()
        {
            const string MARKER = "\n **AUTOCOMPLETE HERE** \n";

            try
            {
                if (showingAutoComplete)
                {
                    return;
                }

                showingAutoComplete = true;
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new();

                CleanCache();

                int caretPosition = view.Caret.Position.BufferPosition.Position;

                string filePath = ((ITextDocument)view.TextDataModel.DocumentBuffer.Properties.GetProperty(typeof(ITextDocument))).FilePath;

                string systemMessage = TextFormat.FormatForCompleteCommand(options.CopilotCommand + Constants.COPILOT_ADDICTIONAL_INSTRUCTIONS, filePath);

                string codeUp = GetCodeUpToCurrentPosition(caretPosition);
                string codeDown = GetCodeBelowCurrentPosition(caretPosition);

                string code = codeUp + MARKER + codeDown;

                string codeNormalized = TextFormat.NormalizeLineBreaks(code);
                codeNormalized = TextFormat.RemoveBlankLinesFromResult(codeNormalized).Trim();

                string cacheKey = $"{filePath}:{codeNormalized}";

                if (cache.TryGetValue(cacheKey, out string cachedPrediction))
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

                    DisplayPrediction(cachedPrediction);
                    return;
                }

                string prediction;

                if (options.CopilotModelOption == CopilotModelOption.Completion && options.Service == OpenAIService.OpenAI)
                {
                    prediction = await ApiHandler.GetCompletionResponseAsync(options, systemMessage, code, null, cancellationTokenSource.Token);
                }
                else
                {
                    string modelOverride = options.CopilotModelOption == CopilotModelOption.Specific ? options.CopilotSpecificModel : null;

                    prediction = await ApiHandler.GetResponseAsync(options, systemMessage, code, null, cancellationTokenSource.Token, null, modelOverride);
                }

                if (cancellationTokenSource.Token.IsCancellationRequested || string.IsNullOrWhiteSpace(prediction))
                {
                    return;
                }

                prediction = FormatPrediction(code, prediction);

                if (string.IsNullOrWhiteSpace(prediction))
                {
                    return;
                }

                cache[cacheKey] = prediction;

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

                DisplayPrediction(prediction);
            }
            catch (OperationCanceledException)
            {
                //Task cancelled - do nothing
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
                showingAutoComplete = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Displays the prediction as ghost text using the <see cref="GhostTextTagger"/>.
        /// Must be called on the UI thread.
        /// </summary>
        /// <param name="prediction">The text to display as ghost text.</param>
        private void DisplayPrediction(string prediction)
        {
            if (view.Properties.TryGetProperty(GhostTextTagger.TaggerKey, out GhostTextTagger tagger))
            {
                int caretPosition = view.Caret.Position.BufferPosition.Position;
                tagger.SetSuggestion(prediction, caretPosition);
            }
        }

        /// <summary>
        /// Retrieves the code from the current caret position up to the start
        /// of the method or the beginning of the document.
        /// </summary>
        /// <param name="caretPosition">The current position of the caret in the text buffer.</param>
        /// <returns>The code from the start of the enclosing method up to the caret.</returns>
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
        /// Retrieves the code from the caret position down to the end of the
        /// current method or document.
        /// </summary>
        /// <param name="caretPosition">The current position of the caret in the text buffer.</param>
        /// <returns>A string containing the code after the caret.</returns>
        private string GetCodeBelowCurrentPosition(int caretPosition)
        {
            ITextSnapshot snapshot = view.TextBuffer.CurrentSnapshot;
            ITextSnapshotLine currentLine = snapshot.GetLineFromPosition(caretPosition);
            StringBuilder codeBelow = new();

            int startLine = currentLine.LineNumber;

            int openBraces = 0;
            bool insideMethod = false;

            for (int i = startLine; i < snapshot.LineCount; i++)
            {
                string lineText = snapshot.GetLineFromLineNumber(i).GetText();
                codeBelow.AppendLine(lineText);

                foreach (char c in lineText)
                {
                    if (c == '{')
                    {
                        openBraces++;
                        insideMethod = true;
                    }
                    else if (c == '}')
                    {
                        openBraces--;
                    }
                }

                if (insideMethod && openBraces <= 0)
                {
                    break; // method body is closed
                }
            }

            return codeBelow.ToString();
        }

        /// <summary>
        /// Removes the original code from the prediction while maintaining
        /// formatting and line breaks.
        /// </summary>
        /// <param name="originalCode">The original code that was sent as context.</param>
        /// <param name="prediction">The prediction returned by the model.</param>
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
        /// Cleans the cache by removing the oldest entries if the cache size
        /// exceeds the maximum limit.
        /// </summary>
        private void CleanCache()
        {
            const int MAX_CACHE_SIZE = 10;

            if (cache.Count > MAX_CACHE_SIZE)
            {
                List<string> keysToRemove = cache.Keys.Take(cache.Count - (MAX_CACHE_SIZE / 2)).ToList();

                foreach (string key in keysToRemove)
                {
                    cache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Restarts the typing debounce timer whenever the user edits the buffer.
        /// </summary>
        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            if (view.Properties.TryGetProperty(GhostTextTagger.TaggerKey, out GhostTextTagger tagger))
            {
                tagger.ClearSuggestion();
            }

            if (suppressNextSuggestion)
            {
                suppressNextSuggestion = false;
                return;
            }

            typingTimer.Stop();
            typingTimer.Start();
        }

        /// <summary>
        /// Triggered when the typing debounce expires; requests a new prediction.
        /// </summary>
        private async void TypingTimer_Tick(object sender, EventArgs e)
        {
            typingTimer.Stop();

            if (!showingAutoComplete)
            {
                await ShowAutocompleteAsync();
            }
        }

        /// <summary>
        /// Cleans up resources when the view is closed.
        /// </summary>
        private void OnViewClosed(object sender, EventArgs e)
        {
            try
            {
                view.TextBuffer.Changed -= TextBuffer_Changed;
                view.Closed -= OnViewClosed;

                if (typingTimer != null)
                {
                    typingTimer.Stop();
                    typingTimer.Tick -= TypingTimer_Tick;
                }

                cancellationTokenSource?.Cancel();

                if (view.Properties.TryGetProperty(GhostTextTagger.TaggerKey, out GhostTextTagger tagger))
                {
                    tagger.ClearSuggestion();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        #endregion
    }
}
