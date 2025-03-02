using JeffPires.VisualChatGPTStudio.Options.ApiAgent;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Windows;

namespace VisualChatGPTStudioShared.Options.ApiAgent
{
    /// <summary>
    /// Represents a handling options within a UI element dialog page for the Agents APIs.
    /// </summary>
    [ComVisible(true)]
    public class OptionApiAgent : UIElementDialogPage
    {
        protected override UIElement Child
        {
            get
            {
                return new OptionApiAgentWindow();
            }
        }
    }
}
