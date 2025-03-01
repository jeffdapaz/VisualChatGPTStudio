using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using JeffPires.VisualChatGPTStudio.ToolWindows;
using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using VisualChatGPTStudioShared.Options.ApiAgent;

namespace JeffPires.VisualChatGPTStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.VisuallChatGPTStudioString)]
    [ProvideOptionPage(typeof(OptionPageGridGeneral), "Visual chatGPT Studio", "General", 0, 0, true)]
    [ProvideProfile(typeof(OptionPageGridGeneral), "Visual chatGPT Studio", "General", 0, 0, true)]
    [ProvideOptionPage(typeof(OptionCommands), "Visual chatGPT Studio", "Commands", 1, 1, true)]
    [ProvideProfile(typeof(OptionCommands), "Visual chatGPT Studio", "Commands", 1, 1, true)]
    [ProvideOptionPage(typeof(OptionApiAgent), "Visual chatGPT Studio", "API Agent", 2, 2, true)]
    [ProvideProfile(typeof(OptionApiAgent), "Visual chatGPT Studio", "API Agent", 2, 2, true)]
    [ProvideToolWindow(typeof(TerminalWindow))]
    [ProvideToolWindow(typeof(TerminalWindowTurbo))]
    [ProvideToolWindow(typeof(TerminalWindowSolutionContext))]
    [ProvideToolWindow(typeof(TerminalWindowCodeReview))]
    public sealed class VisuallChatGPTStudioPackage : ToolkitPackage
    {
        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Gets the OptionPageGridGeneral object.
        /// </summary>
        public OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return (OptionPageGridGeneral)GetDialogPage(typeof(OptionPageGridGeneral));
            }
        }

        /// <summary>
        /// Retrieves the instance of the <see cref="OptionsCommands"/> associated with the dialog page.
        /// </summary>
        /// <returns>
        /// The <see cref="OptionsCommands"/> instance obtained from the dialog page.
        /// </returns>
        public OptionCommands OptionsCommands
        {
            get
            {
                return (OptionCommands)GetDialogPage(typeof(OptionCommands));
            }
        }

        /// <summary>
        /// Retrieves the instance of the <see cref="OptionApiAgent"/> associated with the dialog page.
        /// </summary>
        /// <returns>
        /// The <see cref="OptionApiAgent"/> instance obtained from the dialog page.
        /// </returns>
        public OptionApiAgent OptionApiAgent
        {
            get
            {
                return (OptionApiAgent)GetDialogPage(typeof(OptionApiAgent));
            }
        }

        /// <summary>
        /// Initializes the terminal window commands.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Logger.Initialize(this, Constants.EXTENSION_NAME);

            await this.RegisterCommandsAsync();
            await TerminalWindowCommand.InitializeAsync(this);
            await TerminalWindowTurboCommand.InitializeAsync(this);
            await TerminalWindowSolutionContextCommand.InitializeAsync(this);
            await TerminalWindowCodeReviewCommand.InitializeAsync(this);
        }
    }
}