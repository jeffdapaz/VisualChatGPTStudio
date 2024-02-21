using Newtonsoft.Json;
using System;using System.Collections.Generic;using System.Linq;using System.Windows;using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;using MessageBox = System.Windows.MessageBox;using UserControl = System.Windows.Controls.UserControl;namespace JeffPires.VisualChatGPTStudio.Options.Commands{
    /// <summary>
    /// Represents a user control for displaying and interacting with option commands.
    /// </summary>
    public partial class OptionCommandsWindow : UserControl    {
        #region Delegates
        /// <summary>
        /// Delegate for updating a list of commands.
        /// </summary>
        /// <param name="commands">The list of commands to update.</param>
        public delegate void DelegateUpdateCommands(List<Commands> commands);        public event DelegateUpdateCommands EventUpdateCommands;

        #endregion Delegates
        #region Properties
        private readonly string originalCommands;
        private List<Commands> commands;

        #endregion Properties
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the OptionCommandsWindow class.
        /// </summary>
        /// <param name="commands">The list of commands to display.</param>
        public OptionCommandsWindow(List<Commands> commands)        {            this.InitializeComponent();            this.originalCommands = JsonConvert.SerializeObject(commands);            this.commands = commands;            grdCommands.ItemsSource = this.commands;        }

        #endregion Constructors
        #region Event Handlers
        /// <summary>
        /// Handles the click event of the Cancel button. 
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)        {            if (MessageBox.Show("Cancel all changes?", Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)            {                return;            }            commands = JsonConvert.DeserializeObject<List<Commands>>(originalCommands);            grdCommands.ItemsSource = commands;        }

        /// <summary>
        /// Removes the selected command from the grid.
        /// </summary>
        private void RemoveCommand_Click(object sender, RoutedEventArgs e)        {            if (grdCommands.SelectedItem == null)            {                MessageBox.Show("Please, select a line to be removed.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);                return;            }            if (commands.Count == 1)            {                MessageBox.Show("Must have at least one commands line.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);                return;            }            if (MessageBox.Show("Remove the line?", Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)            {                return;            }            commands.Remove(grdCommands.SelectedItem as Commands);            grdCommands.Items.Refresh();        }

        /// <summary>
        /// Event handler for the AddCommand button click event. Adds a new Commands object to the commands collection and refreshes the grid view.
        /// </summary>
        private void AddCommand_Click(object sender, RoutedEventArgs e)        {            commands.Add(new Commands());            grdCommands.Items.Refresh();        }

        /// <summary>
        /// Saves the commands and updates the command list.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void SaveCommands_Click(object sender, RoutedEventArgs e)        {            try            {                if (commands.Count(c => string.IsNullOrWhiteSpace(c.ProjectName)) != 1)                {                    MessageBox.Show("Must have only one default commands without specifying the project name.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);                    return;                }                if (commands.GroupBy(c => c.ProjectName).Where(c => c.Count() > 1).Any())                {                    MessageBox.Show("Lines with the same Project name are not allowed.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);                    return;                }                EventUpdateCommands?.Invoke(commands);                MessageBox.Show("Commands updated successfully.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Information);            }            catch (Exception ex)            {                Logger.Log(ex);                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Error);            }        }

        #endregion Event Handlers
    }}