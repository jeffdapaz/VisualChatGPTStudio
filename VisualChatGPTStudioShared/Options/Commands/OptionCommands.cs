using Community.VisualStudio.Toolkit;using EnvDTE;using Microsoft.VisualStudio.Shell;using Newtonsoft.Json;using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Runtime.InteropServices;using System.Threading.Tasks;using System.Windows;using System.Windows.Forms;namespace JeffPires.VisualChatGPTStudio.Options.Commands{
    /// <summary>
    /// Represents a class that provides a dialog page for displaying commands options.
    /// </summary>
    [ComVisible(true)]
    public class OptionCommands : UIElementDialogPage    {        private const string CONFIG_FILE_NAME = "commands.json";        private readonly string configFolder;        private readonly string configFilePath;        private List<Commands> commands;

        /// <summary>
        /// Gets the child UIElement for the control.
        /// </summary>
        /// <returns>
        /// The child UIElement, which is an instance of OptionCommandsWindow.
        /// </returns>
        protected override UIElement Child        {            get            {                OptionCommandsWindow window = new(commands);                window.EventUpdateCommands += Window_EventUpdateCommands;                return window;            }        }

        /// <summary>
        /// Initializes a new instance of the OptionCommands class.
        /// </summary>
        public OptionCommands()        {            try            {                configFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Utils.Constants.EXTENSION_NAME);                configFilePath = Path.Combine(configFolder, CONFIG_FILE_NAME);                string commandsAsJson = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : string.Empty;                if (string.IsNullOrWhiteSpace(commandsAsJson))                {                    CreateDefaultCommands();                }                else                {                    commands = JsonConvert.DeserializeObject<List<Commands>>(commandsAsJson);                }                if (commands == null || commands.Count == 0)                {                    CreateDefaultCommands();                }            }            catch (Exception ex)            {                Logger.Log(ex);                System.Windows.Forms.MessageBox.Show(ex.Message, Utils.Constants.EXTENSION_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);            }        }

        /// <summary>
        /// Updates the commands list and saves it to a config file.
        /// </summary>
        /// <param name="commands">The updated list of commands.</param>
        private void Window_EventUpdateCommands(List<Commands> commands)        {            this.commands = commands;            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(commands));        }

        /// <summary>
        /// Retrieves the command associated with the specified command type.
        /// </summary>
        /// <param name="commandType">The type of command to retrieve.</param>
        /// <returns>The command as a string.</returns>
        public async Task<string> GetCommandAsync(CommandsType commandType)        {            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();            DocumentView document = await VS.Documents.GetActiveDocumentViewAsync();            DTE dte = await VS.GetServiceAsync<DTE, DTE>();            ProjectItem projectItem = dte.Solution.FindProjectItem(document.FilePath);            string solutionName = string.Empty;            string projectName = string.Empty;            if (projectItem != null)            {                EnvDTE.Project project = projectItem.ContainingProject;                EnvDTE.Solution solution = project.DTE.Solution;                solutionName = Path.GetFileNameWithoutExtension(solution.FullName);                projectName = project.Name;            }            Commands command = commands.FirstOrDefault(c => c.ProjectName == projectName);            if (command == null)            {                command = commands.FirstOrDefault(c => c.ProjectName == solutionName);            }            if (command == null)            {                command = commands.First(c => string.IsNullOrWhiteSpace(c.ProjectName));            }            string prompt = command.GetType().GetProperty(commandType.ToString()).GetValue(command).ToString();            if (string.IsNullOrWhiteSpace(prompt))
            {
                prompt = commands.First(c => string.IsNullOrWhiteSpace(c.ProjectName)).GetType().GetProperty(commandType.ToString()).GetValue(command).ToString();
            }            return prompt;        }

        /// <summary>
        /// Creates default commands for the application.
        /// </summary>
        private void CreateDefaultCommands()        {            Commands commandsDefault = new()            {                ProjectName = string.Empty,                Complete = "Please complete",                AddTests = "Create unit tests",                FindBugs = "Find Bugs",                Optimize = "Optimize",                Explain = "Explain",                AddSummary = "Only write a comment as C# summary format like",                AddCommentsForLine = "Comment. Add comment char for each comment line",                AddCommentsForLines = "Rewrite the code with comments. Add comment char for each comment line",                Translate = "Translate to English",                CustomBefore = string.Empty,                CustomAfter = string.Empty,                CustomReplace = string.Empty            };            commands = new List<Commands> { commandsDefault };            if (!Directory.Exists(configFolder))            {                Directory.CreateDirectory(configFolder);            }            File.Create(configFilePath).Close();            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(commands));        }    }

    /// <summary>
    /// Enum representing different types of commands.
    /// </summary>
    public enum CommandsType    {        Complete,        AddTests,        FindBugs,        Optimize,        Explain,        AddSummary,        AddCommentsForLine,        AddCommentsForLines,        Translate,        CustomBefore,        CustomAfter,        CustomReplace    }}