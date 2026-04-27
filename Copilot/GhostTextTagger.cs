using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JeffPires.VisualChatGPTStudio.Copilot
{
    /// <summary>
    /// Provides inline ghost-text suggestions as <see cref="IntraTextAdornmentTag"/>
    /// adornments rendered in gray after the caret position.
    /// </summary>
    internal sealed class GhostTextTagger : ITagger<IntraTextAdornmentTag>
    {
        #region Constants

        /// <summary>
        /// Key used to store the tagger instance in the text view's property bag.
        /// </summary>
        internal static readonly object TaggerKey = typeof(GhostTextTagger);

        #endregion

        #region Properties

        private readonly IWpfTextView view;
        private string suggestionText;
        private int suggestionPosition;
        private ITrackingPoint trackingPoint;

        /// <summary>
        /// Raised when the set of tags changes, signaling the editor to re-query <see cref="GetTags"/>.
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GhostTextTagger"/> class
        /// for the specified text view.
        /// </summary>
        /// <param name="view">The text view to provide ghost-text adornments for.</param>
        public GhostTextTagger(IWpfTextView view)
        {
            this.view = view;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Displays the given text as a ghost-text suggestion at the specified buffer position.
        /// </summary>
        /// <param name="text">The suggestion text to render as gray ghost text.</param>
        /// <param name="position">The buffer position where the suggestion starts.</param>
        public void SetSuggestion(string text, int position)
        {
            if (string.IsNullOrEmpty(text))
            {
                ClearSuggestion();
                return;
            }

            ITextSnapshot snapshot = view.TextSnapshot;

            if (position > snapshot.Length)
            {
                position = snapshot.Length;
            }

            suggestionText = text;
            suggestionPosition = position;
            trackingPoint = snapshot.CreateTrackingPoint(position, PointTrackingMode.Negative);

            RaiseTagsChanged();
        }

        /// <summary>
        /// Removes any currently displayed ghost-text suggestion.
        /// </summary>
        public void ClearSuggestion()
        {
            if (suggestionText == null)
            {
                return;
            }

            suggestionText = null;
            trackingPoint = null;

            RaiseTagsChanged();
        }

        /// <summary>
        /// Returns the current suggestion text, or null if no suggestion is active.
        /// </summary>
        public string GetSuggestionText()
        {
            return suggestionText;
        }

        /// <summary>
        /// Accepts the current suggestion by inserting the text into the buffer.
        /// </summary>
        /// <returns>True if a suggestion was accepted; false if no suggestion was active.</returns>
        public bool AcceptSuggestion()
        {
            if (suggestionText == null || trackingPoint == null)
            {
                return false;
            }

            string text = suggestionText;
            ITextSnapshot snapshot = view.TextSnapshot;
            int position = trackingPoint.GetPosition(snapshot);

            ClearSuggestion();

            ITextSnapshot snapshotBefore = view.TextBuffer.CurrentSnapshot;
            view.TextBuffer.Insert(position, text);
            ITextSnapshot snapshotAfter = view.TextBuffer.CurrentSnapshot;

            FormatInsertedText(snapshotAfter, position, text.Length);

            return true;
        }

        /// <summary>
        /// Returns tags for ghost-text adornments within the requested spans.
        /// </summary>
        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (suggestionText == null || trackingPoint == null || spans.Count == 0)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;
            int position = trackingPoint.GetPosition(snapshot);

            if (position > snapshot.Length)
            {
                yield break;
            }

            // Check if the position falls within any of the requested spans.
            SnapshotPoint point = new(snapshot, position);
            bool inRange = false;

            foreach (SnapshotSpan span in spans)
            {
                if (span.Contains(point) || span.End == point)
                {
                    inRange = true;
                    break;
                }
            }

            if (!inRange)
            {
                yield break;
            }

            UIElement adornment = CreateGhostTextElement();

            SnapshotSpan adornmentSpan = new(point, 0);

            yield return new TagSpan<IntraTextAdornmentTag>(
                adornmentSpan,
                new IntraTextAdornmentTag(adornment, null, PositionAffinity.Successor));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the WPF element that renders the ghost text in gray.
        /// </summary>
        private UIElement CreateGhostTextElement()
        {
            string[] lines = suggestionText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (lines.Length == 1)
            {
                return CreateTextBlock(lines[0]);
            }

            StackPanel panel = new() { Orientation = Orientation.Vertical };

            for (int i = 0; i < lines.Length; i++)
            {
                panel.Children.Add(CreateTextBlock(lines[i]));
            }

            return panel;
        }

        /// <summary>
        /// Creates a single gray <see cref="TextBlock"/> for one line of ghost text.
        /// </summary>
        private TextBlock CreateTextBlock(string text)
        {
            TextBlock block = new()
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontFamily = GetEditorFontFamily(),
                FontSize = GetEditorFontSize(),
                Opacity = 0.7
            };

            return block;
        }

        /// <summary>
        /// Gets the font family used by the text editor.
        /// </summary>
        private FontFamily GetEditorFontFamily()
        {
            try
            {
                IWpfTextViewLineCollection lines = view.TextViewLines;

                if (lines != null && lines.IsValid && lines.Count > 0)
                {
                    System.Windows.Media.TextFormatting.TextRunProperties props = lines[0].GetCharacterFormatting(lines[0].Start);

                    if (props != null)
                    {
                        return props.Typeface.FontFamily;
                    }
                }
            }
            catch
            {
                // Fall back to default.
            }

            return new FontFamily("Consolas");
        }

        /// <summary>
        /// Gets the font size used by the text editor.
        /// </summary>
        private double GetEditorFontSize()
        {
            try
            {
                IWpfTextViewLineCollection lines = view.TextViewLines;

                if (lines != null && lines.IsValid && lines.Count > 0)
                {
                    System.Windows.Media.TextFormatting.TextRunProperties props = lines[0].GetCharacterFormatting(lines[0].Start);

                    if (props != null)
                    {
                        return props.FontRenderingEmSize;
                    }
                }
            }
            catch
            {
                // Fall back to default.
            }

            return 13.0;
        }

        /// <summary>
        /// Raises the <see cref="TagsChanged"/> event for the entire visible area.
        /// </summary>
        private void RaiseTagsChanged()
        {
            ITextSnapshot snapshot = view.TextSnapshot;

            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        /// <summary>
        /// Formats the text that was just inserted by selecting the range and
        /// executing the VS "Format Selection" command.
        /// </summary>
        /// <param name="snapshot">The snapshot after the insert.</param>
        /// <param name="startPosition">The start position of the inserted text.</param>
        /// <param name="length">The length of the inserted text.</param>
        private void FormatInsertedText(ITextSnapshot snapshot, int startPosition, int length)
        {
            try
            {
                SnapshotSpan insertedSpan = new(snapshot, startPosition, length);
                view.Selection.Select(insertedSpan, false);

                Guid cmdGroup = VSConstants.VSStd2K;
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

                if (view.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.TextManager.Interop.IVsTextView), out Microsoft.VisualStudio.TextManager.Interop.IVsTextView textViewAdapter))
                {
                    Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget target = (Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget)textViewAdapter;
                    target.Exec(ref cmdGroup, (uint)VSConstants.VSStd2KCmdID.FORMATSELECTION, 0, System.IntPtr.Zero, System.IntPtr.Zero);
                }

                view.Selection.Clear();
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, Math.Min(startPosition + length, view.TextSnapshot.Length)));
            }
            catch (Exception)
            {
                // Formatting is best-effort; do not block the accept.
            }
        }

        #endregion
    }
}
