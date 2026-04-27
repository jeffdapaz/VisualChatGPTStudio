using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace JeffPires.VisualChatGPTStudio.Copilot
{
    /// <summary>
    /// MEF export that creates <see cref="GhostTextTagger"/> instances for
    /// editable text views, enabling inline ghost-text suggestions.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(IntraTextAdornmentTag))]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class GhostTextTaggerProvider : IViewTaggerProvider
    {
        #region Public Methods

        /// <summary>
        /// Creates or retrieves a <see cref="GhostTextTagger"/> for the given text view.
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView is not IWpfTextView wpfView)
            {
                return null;
            }

            if (buffer != textView.TextBuffer)
            {
                return null;
            }

            GhostTextTagger tagger = wpfView.Properties.GetOrCreateSingletonProperty(
                GhostTextTagger.TaggerKey,
                () => new GhostTextTagger(wpfView));

            return tagger as ITagger<T>;
        }

        #endregion
    }
}
