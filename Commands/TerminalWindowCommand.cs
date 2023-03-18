using JeffPires.VisualChatGPTStudio.ToolWindows;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TerminalWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("8b0b1a54-4655-4dae-8984-022f82a739f2");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// This field holds a reference to the TerminalWindow object.
        /// </summary>
        private static TerminalWindow window;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TerminalWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            CommandID menuCommandID = new CommandID(CommandSet, CommandId);
            MenuCommand menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TerminalWindowCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in TerminalWindowCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new TerminalWindowCommand(package, commandService);

            await InitializeToolWindowAsync(package);
        }

        /// <summary>
        /// Sends a request to the ChatGPT window.
        /// </summary>
        /// <param name="command">The command to send to the ChatGPT window.</param>
        public async Task RequestToWindowAsync(string command)
        {
            if (window == null)
            {
                throw new Exception("Please, open the tool window first.");
            }

            await window.RequestToWindowAsync(command);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            _ = this.package.JoinableTaskFactory.RunAsync(async delegate
            {
                await InitializeToolWindowAsync(this.package);
            });
        }

        /// <summary>
        /// Initializes the ToolWindow with the specified <paramref name="package"/>. 
        /// </summary>
        /// <param name="package">The AsyncPackage to be initialized.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task InitializeToolWindowAsync(AsyncPackage package)
        {
            window = await package.ShowToolWindowAsync(typeof(TerminalWindow), 0, true, package.DisposalToken) as TerminalWindow;

            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            window.SetTerminalWindowProperties(((VisuallChatGPTStudioPackage)package).OptionsGeneral, package);
        }
    }
}
