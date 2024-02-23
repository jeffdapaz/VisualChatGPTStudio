﻿using Community.VisualStudio.Toolkit;
using System.Collections.Generic;
using CheckBox = System.Windows.Controls.CheckBox;
using Project = EnvDTE.Project;
    /// <summary>
    /// Represents a user control for the Terminal Window Solution Context.
    /// </summary>
    public partial class TerminalWindowSolutionContextControl : UserControl
        #region Properties
        private DTE dte;
        private readonly List<string> validProjectTypes;
        private readonly List<string> validProjectItems;
        private readonly List<string> invalidProjectItems;

        #endregion Properties
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the TerminalWindowSolutionContextControl class.
        /// </summary>
        public TerminalWindowSolutionContextControl()

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
            };

            invalidProjectItems = new()
            {
                ".png",
                ".bmp",
                ".exe",
                ".dll",
                ".suo",
                ".vsct",
                ".vssscc",
                ".vspscc",
                ".user",
                ".vsixmanifest",
                ".pdb"
            };

        #endregion Constructors
        #region Event Handlers

        /// <summary>
        /// Event handler for the btnRefresh button click event. 
        /// Retrieves the current text color from the application resources and sets it as the foreground color for the button. 
        /// Calls the PopulateTreeViewAsync method to populate the tree view asynchronously.
        /// </summary>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)

        #endregion Event Handlers
        #region Methods

        /// <summary>
        /// Populates the TreeView asynchronously with the solution and project items.
        /// </summary>
        private async System.Threading.Tasks.Task PopulateTreeViewAsync()
                {
                    continue;
                }

        /// <summary>
        /// Populates the project items in a tree view.
        /// </summary>
        /// <param name="items">The project items to populate.</param>
        /// <param name="parentNode">The parent node in the tree view.</param>
        private void PopulateProjectItems(ProjectItems items, TreeViewItem parentNode)

            foreach (ProjectItem item in items)
                    !item.Kind.Equals(Constants.vsProjectItemKindPhysicalFile) &&
                    !item.Kind.Equals(Constants.vsProjectItemKindPhysicalFolder) &&
                    !item.Kind.Equals(Constants.vsProjectItemKindVirtualFolder))
                {
                    continue;
                }
                    (item.ProjectItems == null || item.ProjectItems.Count == 0))
                {
                    continue;
                }

                if (invalidProjectItems.Any(i => item.Name.EndsWith(i)))
                {
                    continue;
                }

                parentNode.Items.Add(itemNode);

                PopulateProjectItems(item.ProjectItems, itemNode);
                PopulateProjectItems(item.SubProject?.ProjectItems, itemNode);
            }

        /// <summary>
        /// Recursively checks or unchecks all child items of a TreeViewItem based on the provided isChecked value.
        /// </summary>
        /// <param name="parentItem">The parent TreeViewItem.</param>
        /// <param name="isChecked">The value indicating whether the child items should be checked or unchecked.</param>
        private void CheckChildItems(TreeViewItem parentItem, bool isChecked)

        /// <summary>
        /// Sets up a TreeViewItem with a CheckBox as its header.
        /// </summary>
        /// <param name="name">The name to be displayed in the CheckBox.</param>
        /// <returns>The configured TreeViewItem.</returns>
        private TreeViewItem SetupTreeViewItem(string name)
            {
                Orientation = Orientation.Horizontal
            };
            {
                Source = GetFileIcon(name),
                Width = 20,
                Height = 20
            };

            stackPanel.Children.Add(iconImage);

            itemNode.Header = stackPanel;

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
        public List<string> GetSelectedFilesName()
            {
                return new List<string>();
            }

        /// <summary>
        /// Retrieves the names of selected files from a TreeViewItem.
        /// </summary>
        /// <param name="root">The root TreeViewItem.</param>
        /// <returns>A list of selected file names.</returns>
        private List<string> GetSelectedFilesName(TreeViewItem root)

        /// <summary>
        /// Finds and returns the CheckBox control within a TreeViewItem.
        /// </summary>
        /// <param name="item">The TreeViewItem to search within.</param>
        /// <returns>The CheckBox control if found, otherwise null.</returns>
        private CheckBox FindCheckBox(TreeViewItem item)
                {
                    return checkbox;
                }
            }

        #endregion Methods     
    }