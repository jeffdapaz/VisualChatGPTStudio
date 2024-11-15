using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
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
    [Guid("51d8ca1e-4698-404c-a37b-a4a41603557b")]
    public class TerminalWindowTurbo : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowTurbo"/> class.
        /// </summary>
        public TerminalWindowTurbo() : base(null)
        {
            this.Caption = "Visual chatGPT Studio Turbo";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new TerminalWindowTurboControl();
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            ((TerminalWindowTurboControl)this.Content).StartControl(((VisuallChatGPTStudioPackage)this.Package).OptionsGeneral, (Package)this.Package);
        }
    }
}
