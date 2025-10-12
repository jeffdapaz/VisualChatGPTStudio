using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Copilot
{
    /// <summary>
    /// Represents a command filter that intercepts and processes specific commands in the text editor.
    /// </summary>
    internal class CommandFilter : IOleCommandTarget
    {
        private readonly IWpfTextView _textView;
        private readonly InlinePredictionManager _predictionManager;
        private IOleCommandTarget _nextCommandTarget;
        private readonly OptionPageGridGeneral _options;

        /// <summary>
        /// Initializes a new instance of the CommandFilter class.
        /// </summary>
        /// <param name="textView">The text view associated with this command filter.</param>
        /// <param name="predictionManager">The manager responsible for handling inline predictions.</param>
        /// <param name="options">The general options.</param>
        public CommandFilter(IWpfTextView textView, InlinePredictionManager predictionManager, OptionPageGridGeneral options)
        {
            _textView = textView;
            _predictionManager = predictionManager;
            _options = options;
        }

        /// <summary>
        /// Attaches the command filter to the given text view adapter.
        /// </summary>
        /// <param name="textViewAdapter">The text view adapter to attach the command filter to.</param>
        public void AttachToView(IVsTextView textViewAdapter)
        {
            textViewAdapter.AddCommandFilter(this, out _nextCommandTarget);
        }

        /// <summary>
        /// Queries the status of one or more commands.
        /// </summary>
        /// <param name="pguidCmdGroup">The unique identifier of the command group.</param>
        /// <param name="cCmds">The number of commands in the prgCmds array.</param>
        /// <param name="prgCmds">An array of OLECMD structures that indicate the commands for which the caller needs status information.</param>
        /// <param name="pCmdText">A pointer to an OLECMDTEXT structure in which to return name and/or status information of a single command.</param>
        /// <returns>
        /// Returns an integer value indicating the success or failure of the method call.
        /// </returns>
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        /// <summary>
        /// Executes a command within the Visual Studio environment, with special handling for Enter and Tab keys when Copilot is enabled.
        /// </summary>
        /// <param name="pguidCmdGroup">The command group GUID.</param>
        /// <param name="nCmdID">The command ID.</param>
        /// <param name="nCmdexecopt">The command execution options.</param>
        /// <param name="pvaIn">Pointer to input arguments.</param>
        /// <param name="pvaOut">Pointer to output arguments.</param>
        /// <returns>
        /// An integer indicating the result of the command execution.
        /// </returns>
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_options.CopilotEnabled && pguidCmdGroup == VSConstants.VSStd2K)
            {
                if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
                {
                    ProcessEnterKey(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
                else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
                {
                    return ProcessTabKey(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        /// <summary>
        /// Processes the Enter key press event triggering the suggestion insertion asynchronously.
        /// </summary>
        /// <param name="pguidCmdGroup">The command group GUID.</param>
        /// <param name="nCmdID">The command ID.</param>
        /// <param name="nCmdexecopt">The command execution options.</param>
        /// <param name="pvaIn">Pointer to input arguments.</param>
        /// <param name="pvaOut">Pointer to output arguments.</param>
        private void ProcessEnterKey(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            _predictionManager.ShowAutocompleteAsync();
        }

        /// <summary>
        /// Processes the Tab key by first allowing the editor to handle it and then formatting the code asynchronously.
        /// </summary>
        /// <param name="pguidCmdGroup">The command group GUID.</param>
        /// <param name="nCmdID">The command ID.</param>
        /// <param name="nCmdexecopt">The command execution options.</param>
        /// <param name="pvaIn">Pointer to input arguments.</param>
        /// <param name="pvaOut">Pointer to output arguments.</param>
        /// <returns>
        /// The result of the command execution.
        /// </returns>
        private int ProcessTabKey(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Let the editor process the Tab key first.
            int result = _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            // After the editor processes the Tab key, format the code.
            Task.Run(async () => { _predictionManager.OnTabPressedAsync(); });

            return result;
        }
    }
}