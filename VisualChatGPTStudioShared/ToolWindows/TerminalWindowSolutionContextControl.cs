using Community.VisualStudio.Toolkit;using EnvDTE;using Microsoft.VisualStudio.Shell;using System;
using System.Collections.Generic;using System.Linq;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Imaging;
using CheckBox = System.Windows.Controls.CheckBox;using Path = System.IO.Path;
using Project = EnvDTE.Project;using Solution = EnvDTE.Solution;using UserControl = System.Windows.Controls.UserControl;namespace JeffPires.VisualChatGPTStudio.ToolWindows{
    /// <summary>
    /// Represents a user control for the Terminal Window Solution Context.
    /// </summary>
    public partial class TerminalWindowSolutionContextControl : UserControl    {
        #region Properties
        private DTE dte;        private SolidColorBrush foreGroundColor;
        private readonly List<string> validProjectTypes;
        private readonly List<string> validProjectItems;

        #endregion Properties
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the TerminalWindowSolutionContextControl class.
        /// </summary>
        public TerminalWindowSolutionContextControl()        {            ThreadHelper.ThrowIfNotOnUIThread();            this.InitializeComponent();

            validProjectTypes = new()
            {
                ".csproj",
                ".vbproj",
                ".vcxproj",
                ".fsproj",
                ".pyproj",
                ".jsproj",
                ".sqlproj",
                ".wixproj",
                ".njsproj",
                ".shproj"
            };

            validProjectItems = new()
            {
                ".config",
                ".cs",
                ".css",
                ".html",
                ".js",
                ".json",
                ".md",
                ".sql",
                ".ts",
                ".vb",
                ".xml",
                ".xaml"
            };            _ = PopulateTreeViewAsync();        }

        #endregion Constructors
        #region Event Handlers

        /// <summary>
        /// Event handler for the btnRefresh button click event. 
        /// Retrieves the current text color from the application resources and sets it as the foreground color for the button. 
        /// Calls the PopulateTreeViewAsync method to populate the tree view asynchronously.
        /// </summary>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)        {            Color textColor = ((SolidColorBrush)Application.Current.Resources[VsBrushes.WindowTextKey]).Color;            foreGroundColor = new SolidColorBrush(textColor);            _ = PopulateTreeViewAsync();        }

        #endregion Event Handlers
        #region Methods

        /// <summary>
        /// Populates the TreeView asynchronously with the solution and project items.
        /// </summary>
        private async System.Threading.Tasks.Task PopulateTreeViewAsync()        {            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();            dte ??= await VS.GetServiceAsync<DTE, DTE>();            Solution solution = dte.Solution;            string solutionName = Path.GetFileName(solution.FullName);            if (string.IsNullOrWhiteSpace(solutionName))            {                return;            }            TreeViewItem solutionNode;            if (treeView.Items.Count > 0)            {                treeView.Items.Clear();            }            solutionNode = SetupTreeViewItem(solutionName);            treeView.Items.Add(solutionNode);            foreach (Project project in solution.Projects)            {                if (string.IsNullOrWhiteSpace(project?.FileName))
                {
                    continue;
                }                TreeViewItem projectNode = SetupTreeViewItem(Path.GetFileName(project.FileName));                solutionNode.Items.Add(projectNode);                PopulateProjectItems(project.ProjectItems, projectNode);            }        }

        /// <summary>
        /// Populates the project items in a tree view.
        /// </summary>
        /// <param name="items">The project items to populate.</param>
        /// <param name="parentNode">The parent node in the tree view.</param>
        private void PopulateProjectItems(ProjectItems items, TreeViewItem parentNode)        {            if (items == null)            {                return;            }            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in items)            {                if (!validProjectItems.Any(i => item.Name.EndsWith(i)) &&
                    !item.Kind.Equals(Constants.vsProjectItemKindPhysicalFolder) &&
                    !item.Kind.Equals(Constants.vsProjectItemKindVirtualFolder))
                {
                    continue;
                }                if ((item.Kind.Equals(Constants.vsProjectItemKindPhysicalFolder) || item.Kind.Equals(Constants.vsProjectItemKindVirtualFolder)) &&
                    !CanAddProjectFolder(item.ProjectItems))
                {
                    continue;
                }                TreeViewItem itemNode = SetupTreeViewItem(item.Name);

                parentNode.Items.Add(itemNode);

                PopulateProjectItems(item.ProjectItems, itemNode);
            }        }

        /// <summary>
        /// Checks if a project folder can be added.
        /// </summary>
        /// <param name="folderItems">The project items in the folder.</param>
        /// <returns>True if a project folder can be added, false otherwise.</returns>
        private bool CanAddProjectFolder(ProjectItems folderItems)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (folderItems == null || folderItems.Count == 0)
            {
                return false;
            }

            foreach (ProjectItem folderItem in folderItems)
            {
                if (validProjectItems.Any(i => folderItem.Name.EndsWith(i)))
                {
                    return true;
                }
                else if (folderItem.Kind.Equals(Constants.vsProjectItemKindPhysicalFolder) || folderItem.Kind.Equals(Constants.vsProjectItemKindVirtualFolder))
                {
                    return CanAddProjectFolder(folderItem.ProjectItems);
                }
            }

            return false;
        }

        /// <summary>
        /// Recursively checks or unchecks all child items of a TreeViewItem based on the provided isChecked value.
        /// </summary>
        /// <param name="parentItem">The parent TreeViewItem.</param>
        /// <param name="isChecked">The value indicating whether the child items should be checked or unchecked.</param>
        private void CheckChildItems(TreeViewItem parentItem, bool isChecked)        {            foreach (object item in parentItem.Items)            {                if (item is TreeViewItem treeViewItem)                {                    CheckBox checkbox = FindCheckBox(treeViewItem);                    checkbox.IsChecked = isChecked;                    CheckChildItems(treeViewItem, isChecked);                }            }        }

        /// <summary>
        /// Sets up a TreeViewItem with a CheckBox as its header.
        /// </summary>
        /// <param name="name">The name to be displayed in the CheckBox.</param>
        /// <returns>The configured TreeViewItem.</returns>
        private TreeViewItem SetupTreeViewItem(string name)        {            TreeViewItem itemNode = new();            StackPanel stackPanel = new()
            {
                Orientation = Orientation.Horizontal
            };            Image iconImage = new()
            {
                Source = GetFileIcon(name),
                Width = 15,
                Height = 15
            };

            stackPanel.Children.Add(iconImage);            CheckBox checkBox = new()            {                Content = name,                IsChecked = false,                Foreground = foreGroundColor,                FontSize = 15,                Margin = new Thickness(5, 5, 0, 0)            };            stackPanel.Children.Add(checkBox);

            itemNode.Header = stackPanel;            itemNode.IsExpanded = false;            checkBox.Checked += (sender, e) =>            {                CheckChildItems(itemNode, true);            };            checkBox.Unchecked += (sender, e) =>            {                CheckChildItems(itemNode, false);            };            return itemNode;        }

        /// <summary>
        /// Retrieves the icon image source for a given file based on its extension.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The image source of the file icon.</returns>
        private ImageSource GetFileIcon(string fileName)
        {
            BitmapImage imageSource = new();
            string fileExtension = string.Empty;

            try
            {
                fileExtension = Path.GetExtension(fileName);
            }
            catch (Exception)
            {

            }

            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = "folder";
            }
            else if (fileExtension == ".sln")
            {
                fileExtension = "sln";
            }
            else if (validProjectTypes.Any(i => i == fileExtension))
            {
                fileExtension = "vs";
            }
            else if (!validProjectItems.Any(i => i == fileExtension))
            {
                return imageSource;
            }

            string uriSource = $"pack://application:,,,/VisualChatGPTStudio;component/Resources/FileTypes/{fileExtension.Replace(".", string.Empty)}.png";

            imageSource.BeginInit();
            imageSource.UriSource = new Uri(uriSource);
            imageSource.EndInit();

            return imageSource;
        }

        /// <summary>
        /// Retrieves the names of the selected files in a tree view.
        /// </summary>
        /// <returns>
        /// A list of strings containing the names of the selected files.
        /// </returns>
        public List<string> GetSelectedFilesName()        {            return GetSelectedFilesName((TreeViewItem)treeView.Items.GetItemAt(0));        }

        /// <summary>
        /// Retrieves the names of selected files from a TreeViewItem.
        /// </summary>
        /// <param name="root">The root TreeViewItem.</param>
        /// <returns>A list of selected file names.</returns>
        private List<string> GetSelectedFilesName(TreeViewItem root)        {            List<string> selectedFilesName = new();            foreach (object item in root.Items)            {                if (item is TreeViewItem treeViewItem)                {                    CheckBox checkBox = FindCheckBox(treeViewItem);                    if (checkBox != null && checkBox.IsChecked == true)                    {                        selectedFilesName.Add(checkBox.Content.ToString());                    }                    selectedFilesName.AddRange(GetSelectedFilesName(treeViewItem));                }            }            return selectedFilesName;        }

        /// <summary>
        /// Finds and returns the CheckBox control within a TreeViewItem.
        /// </summary>
        /// <param name="item">The TreeViewItem to search within.</param>
        /// <returns>The CheckBox control if found, otherwise null.</returns>
        private CheckBox FindCheckBox(TreeViewItem item)        {            if (item.Header is StackPanel stackPanel)            {                if (stackPanel.Children?[1] is CheckBox checkbox)
                {
                    return checkbox;
                }
            }            foreach (object subItem in item.Items)            {                if (subItem is TreeViewItem subTreeViewItem)                {                    CheckBox subCheckBox = FindCheckBox(subTreeViewItem);                    if (subCheckBox != null)                    {                        return subCheckBox;                    }                }            }            return null;        }

        #endregion Methods     
    }}