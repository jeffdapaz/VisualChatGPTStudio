using JeffPires.VisualChatGPTStudio.Options.ApiAgent;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Windows;

namespace VisualChatGPTStudioShared.Options.ApiAgent
{
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
