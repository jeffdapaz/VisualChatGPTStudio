using JeffPires.VisualChatGPTStudio.Options.McpAgent;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Windows;

namespace VisualChatGPTStudioShared.Options.McpAgent
{
    /// <summary>
    /// Represents a handling options within a UI element dialog page for the MCP servers.
    /// </summary>
    [ComVisible(true)]
    public class OptionMcpAgent : UIElementDialogPage
    {
        /// <summary>
        /// Gets the UI element hosted in this option page.
        /// </summary>
        protected override UIElement Child
        {
            get
            {
                return new OptionMcpAgentWindow();
            }
        }
    }
}
