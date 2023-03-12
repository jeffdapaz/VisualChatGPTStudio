global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.ToolWindows;
using System.Runtime.InteropServices;
using System.Threading;

namespace JeffPires.VisualChatGPTStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.VisuallChatGPTStudioString)]
    [ProvideOptionPage(typeof(OptionPageGrid), "Visual chatGPT Studio", "General", 0, 0, true)]
    [ProvideProfile(typeof(OptionPageGrid), "Visual chatGPT Studio", "General", 0, 0, true)]
    [ProvideToolWindow(typeof(TerminalWindow))]
    [ProvideToolWindow(typeof(TerminalWindowTurbo))]
    public sealed class VisuallChatGPTStudioPackage : ToolkitPackage
    {
        public OptionPageGrid Options
        {
            get
            {
                return (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
            await TerminalWindowCommand.InitializeAsync(this);
            await TerminalWindowTurboCommand.InitializeAsync(this);
        }
    }
}