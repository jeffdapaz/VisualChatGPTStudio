using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.ToolWindows;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TerminalWindowSolutionContextCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 258;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new("8b0b1a54-4655-4dae-8984-022f82a739f2");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// This field holds a reference to the TerminalWindow object.
        /// </summary>
        private static TerminalWindowSolutionContext window;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TerminalWindowSolutionContextCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static TerminalWindowSolutionContextCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in TerminalWindowCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new TerminalWindowSolutionContextCommand(package, commandService);
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
        private static async System.Threading.Tasks.Task InitializeToolWindowAsync(AsyncPackage package)
        {
            window = await package.ShowToolWindowAsync(typeof(TerminalWindowSolutionContext), 0, true, package.DisposalToken) as TerminalWindowSolutionContext;

            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
        }

        /// <summary>
        /// Retrieves the code content of selected context items in the solution.
        /// </summary>
        /// <returns>
        /// A list of strings containing the code content of the selected context items.
        /// </returns>
        public async Task<List<string>> GetSelectedContextItemsCodeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            List<string> result = new();

            List<string> selectedFilesName = ((TerminalWindowSolutionContextControl)window.Content).GetSelectedFilesName();

            string fileContent = string.Empty;

            foreach (string itemName in selectedFilesName)
            {
                ProjectItem projectItem = await FindProjectItemInSolutionAsync(itemName);

                if (projectItem == null || !File.Exists(projectItem.FileNames[1]))
                {
                    continue;
                }

                try
                {
                    fileContent = File.ReadAllText(projectItem.FileNames[1]);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }

                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    result.Add(fileContent);
                }
            }

            return result;
        }

        /// <summary>
        /// Asynchronously finds a project item in the solution by its name.
        /// </summary>
        /// <param name="itemName">The name of the project item to find.</param>
        /// <returns>The project item if found, otherwise null.</returns>
        private async Task<ProjectItem> FindProjectItemInSolutionAsync(string itemName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (EnvDTE.Project project in (await VS.GetServiceAsync<DTE, DTE>()).Solution.Projects)
            {
                ProjectItem projectItem = FindProjectItemByName(project.ProjectItems, itemName);

                if (projectItem != null)
                {
                    return projectItem;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a project item by name in a collection of project items.
        /// </summary>
        /// <param name="projectItems">The collection of project items to search.</param>
        /// <param name="itemName">The name of the project item to find.</param>
        /// <returns>The project item with the specified name, or null if not found.</returns>
        private ProjectItem FindProjectItemByName(ProjectItems projectItems, string itemName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItems == null)
            {
                return null;
            }

            foreach (ProjectItem projectItem in projectItems)
            {
                if (projectItem.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                {
                    return projectItem;
                }

                ProjectItem subItem = FindProjectItemByName(projectItem.ProjectItems, itemName);

                if (subItem == null)
                {
                    subItem = FindProjectItemByName(projectItem.SubProject?.ProjectItems, itemName);
                }

                if (subItem != null)
                {
                    return subItem;
                }
            }

            return null;
        }
    }
}
