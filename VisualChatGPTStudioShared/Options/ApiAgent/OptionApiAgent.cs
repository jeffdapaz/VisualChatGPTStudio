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

        /// <summary>
        /// Called when the user clicks the OK or Apply button.
        /// </summary>
        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                // You can access the child window and save data here
                if (Child is OptionApiAgentWindow window)
                {
                    window.SaveApiDefinitions();
                }
            }
        }
    }
}
