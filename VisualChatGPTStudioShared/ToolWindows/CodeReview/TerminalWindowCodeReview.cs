using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace JeffPires.VisualChatGPTStudio.ToolWindows
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("3CF59F3B-AB43-4723-A204-C1473A2C7F44")]
    public class TerminalWindowCodeReview : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowCodeReview"/> class.
        /// </summary>
        public TerminalWindowCodeReview() : base(null)
        {
            this.Caption = "Visual chatGPT Studio Code Review";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new TerminalWindowCodeReviewControl();
        }

        /// <summary>
        /// Sets the terminal window properties.
        /// </summary>
        /// <param name="options">The options.</param>
        public void SetTerminalWindowProperties(OptionPageGridGeneral options)
        {
            ((TerminalWindowCodeReviewControl)this.Content).StartControl(options);
        }
    }
}
