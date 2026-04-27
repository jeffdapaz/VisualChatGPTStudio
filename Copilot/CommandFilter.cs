using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace JeffPires.VisualChatGPTStudio.Copilot
{
    /// <summary>
    /// Command filter that intercepts Tab and Escape keystrokes to accept
    /// or dismiss inline ghost-text suggestions provided by <see cref="GhostTextTagger"/>.
    /// </summary>
    internal sealed class CommandFilter : IOleCommandTarget
    {
        #region Properties

        private readonly IWpfTextView view;
        private readonly IOleCommandTarget nextCommandTarget;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandFilter"/> class
        /// and attaches it to the command chain of the given text view.
        /// </summary>
        /// <param name="view">The WPF text view to filter commands for.</param>
        /// <param name="textViewAdapter">The legacy text view adapter used to register the filter.</param>
        public CommandFilter(IWpfTextView view, IVsTextView textViewAdapter)
        {
            this.view = view;

            textViewAdapter.AddCommandFilter(this, out nextCommandTarget);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes incoming commands, intercepting Tab to accept and Escape
        /// to dismiss ghost-text suggestions.
        /// </summary>
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
                {
                    if (TryAcceptSuggestion())
                    {
                        return VSConstants.S_OK;
                    }
                }
                else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
                {
                    if (TryDismissSuggestion())
                    {
                        return VSConstants.S_OK;
                    }
                }
            }

            int result = nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
            {
                RestartAutocompleteTimer();
            }

            return result;
        }

        /// <summary>
        /// Queries the status of a command, reporting Tab and Escape as
        /// supported when a ghost-text suggestion is active.
        /// </summary>
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    if (prgCmds[i].cmdID == (uint)VSConstants.VSStd2KCmdID.TAB ||
                        prgCmds[i].cmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
                    {
                        if (HasActiveSuggestion())
                        {
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                        }
                    }
                }
            }

            return nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Restarts the autocomplete debounce timer after an Enter keystroke.
        /// </summary>
        private void RestartAutocompleteTimer()
        {
            if (view.Properties.TryGetProperty(typeof(InlinePredictionManager), out InlinePredictionManager manager))
            {
                manager.RestartTimer();
            }
        }

        /// <summary>
        /// Checks whether a ghost-text suggestion is currently displayed.
        /// </summary>
        private bool HasActiveSuggestion()
        {
            if (view.Properties.TryGetProperty(GhostTextTagger.TaggerKey, out GhostTextTagger tagger))
            {
                return tagger.GetSuggestionText() != null;
            }

            return false;
        }

        /// <summary>
        /// Attempts to accept the current ghost-text suggestion by inserting it
        /// into the text buffer.
        /// </summary>
        /// <returns>True if a suggestion was accepted; false otherwise.</returns>
        private bool TryAcceptSuggestion()
        {
            if (view.Properties.TryGetProperty(GhostTextTagger.TaggerKey, out GhostTextTagger tagger))
            {
                if (tagger.AcceptSuggestion())
                {
                    if (view.Properties.TryGetProperty(typeof(InlinePredictionManager), out InlinePredictionManager manager))
                    {
                        manager.NotifySuggestionAccepted();
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to dismiss the current ghost-text suggestion.
        /// </summary>
        /// <returns>True if a suggestion was dismissed; false otherwise.</returns>
        private bool TryDismissSuggestion()
        {
            if (view.Properties.TryGetProperty(GhostTextTagger.TaggerKey, out GhostTextTagger tagger))
            {
                if (tagger.GetSuggestionText() != null)
                {
                    tagger.ClearSuggestion();
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
