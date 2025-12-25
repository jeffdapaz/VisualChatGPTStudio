using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Provides helper methods for operations related to the Visual Studio Solution Explorer.
    /// </summary>
    public static class SolutionExplorerHelper
    {
        #region Public Methods

        /// <summary>
        /// Asynchronously retrieves the structure of the current Visual Studio solution as a formatted JSON string.
        /// </summary>
        /// <returns>
        /// A JSON string representing the solution name and its projects.
        /// </returns>
        public static async System.Threading.Tasks.Task<string> GetSolutionStructureJsonAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            EnvDTE.Solution solution = dte.Solution;

            var solutionObj = new
            {
                Solution = new
                {
                    SolutionName = Path.GetFileNameWithoutExtension(solution.FullName),
                    Projects = GetProjects(solution.Projects)
                }
            };

            return JsonConvert.SerializeObject(solutionObj, Formatting.Indented);
        }

        /// <summary>
        /// Searches for a project item within the current solution by a given file path.
        /// </summary>
        /// <param name="filePath">The full file path to search for.</param>
        /// <returns>
        /// Returns the <see cref="ProjectItem"/> corresponding to the specified file path if found; otherwise, returns null.
        /// </returns>
        public static async Task<ProjectItem> FindProjectItemByPathAsync(string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                ProjectItem item = FindProjectItemRecursive(project.ProjectItems, filePath);

                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Asynchronously retrieves the content of a specified <see cref="ProjectItem"/>. If the item is open or can be opened as a document,
        /// it reads the content using the TextDocument API; otherwise, it attempts to read the file contents directly from disk.
        /// Returns an empty string if content cannot be retrieved.
        /// </summary>
        /// <param name="item">The <see cref="ProjectItem"/> whose content is to be fetched.</param>
        /// <returns>
        /// A <see cref="Task{String}"/> representing the asynchronous operation, with the file's content as the result.
        /// </returns>
        public static async Task<string> GetProjectItemContentAsync(ProjectItem item)
        {
            Window window = null;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                if (!item.IsOpen)
                {
                    window = item.Open();
                }

                Document doc = item.Document ?? window?.Document;

                if (doc != null)
                {
                    if (doc.Object("TextDocument") is TextDocument textDoc)
                    {
                        EditPoint editPoint = textDoc.StartPoint.CreateEditPoint();

                        return editPoint.GetText(textDoc.EndPoint);
                    }
                }
            }
            catch { /* fallback below */ }

            if (item.FileCount > 0)
            {
                string path = item.FileNames[1];

                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
            }

            return string.Empty;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Retrieves a list of objects representing projects within the given <paramref name="projects"/>, 
        /// including the project's name, base path, and its files from project items.
        /// Projects with empty or whitespace-only paths are skipped.
        /// </summary>
        /// <param name="projects">A collection of EnvDTE.Project instances to process.</param>
        /// <returns>
        /// A list of anonymous objects, each containing the project's name, path (without extension), 
        /// and a list of files obtained from its project items.
        /// </returns>
        private static List<object> GetProjects(Projects projects)
        {
            List<object> projectList = [];

            foreach (EnvDTE.Project project in projects)
            {
                if (string.IsNullOrWhiteSpace(project.FullName))
                {
                    continue;
                }

                string projectBasePath = Path.GetDirectoryName(project.FullName);

                projectList.Add(new
                {
                    ProjectName = project.Name,
                    Path = Path.GetFileNameWithoutExtension(project.FullName),
                    Files = GetFilesFromProjectItems(project.ProjectItems, projectBasePath)
                });
            }
            return projectList;
        }

        /// <summary>
        /// Recursively collects the relative file paths of all physical files within the given project items, excluding ignored files or folders.
        /// </summary>
        /// <param name="items">The collection of project items to search through.</param>
        /// <param name="basePath">The base directory path used for generating relative file paths.</param>
        /// <returns>
        /// A list of relative file paths (with forward slashes) for all non-ignored physical files found within the specified project items.
        /// </returns>
        private static List<string> GetFilesFromProjectItems(ProjectItems items, string basePath)
        {
            List<string> list = [];

            foreach (ProjectItem item in items)
            {
                try
                {
                    if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile && item.FileCount > 0)
                    {
                        string filePath = item.FileNames[1];

                        if (!IsIgnoredFileOrFolder(filePath))
                        {
                            string relative = MakeRelativePath(basePath, filePath);
                            list.Add(relative.Replace("\\", "/"));
                        }
                    }

                    else if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                    {
                        list.AddRange(GetFilesFromProjectItems(item.ProjectItems, basePath));
                    }
                }
                catch { /* safe for unloaded or broken items */ }
            }

            return list;
        }

        /// <summary>
        /// Determines whether the specified path refers to a file or folder that should be ignored,
        /// such as build directories or version control folders.
        /// </summary>
        /// <param name="path">The file or folder path to check.</param>
        /// <returns>
        /// True if the path contains any ignored folders ("bin", "obj", ".vs", ".git", ".vscode", "node_modules");
        /// otherwise, false.
        /// </returns>
        private static bool IsIgnoredFileOrFolder(string path)
        {
            string[] ignoredFolders = ["bin", "obj", ".vs", ".git", ".vscode", "node_modules"];

            foreach (string ignored in ignoredFolders)
            {
                if (path.Split(Path.DirectorySeparatorChar).Any(p => p.Equals(ignored, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a relative path from the specified <paramref name="basePath"/> to the given <paramref name="fullPath"/>.
        /// </summary>
        /// <param name="basePath">The base directory path from which the relative path is calculated.</param>
        /// <param name="fullPath">The full absolute path to the target file or directory.</param>
        /// <returns>
        /// A relative path string from <paramref name="basePath"/> to <paramref name="fullPath"/>. If the paths do not share a common root, returns the original <paramref name="fullPath"/>.
        /// </returns>
        private static string MakeRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
            Uri fileUri = new(fullPath);

            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
        }

        /// <summary>
        /// Recursively searches through ProjectItems to find a ProjectItem that matches the specified file path.
        /// </summary>
        /// <param name="items">The collection of ProjectItems to search.</param>
        /// <param name="filePath">The file path to match against the ProjectItems.</param>
        /// <returns>
        /// The matching ProjectItem if found; otherwise, null.
        /// </returns>
        private static ProjectItem FindProjectItemRecursive(ProjectItems items, string filePath)
        {
            foreach (ProjectItem item in items)
            {
                if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                {
                    if (item.FileCount > 0)
                    {
                        string itemPath = item.FileNames[1];

                        if (ArePathsEqual(itemPath, filePath))
                        {
                            return item;
                        }
                    }
                }

                if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                {
                    ProjectItem child = FindProjectItemRecursive(item.ProjectItems, filePath);

                    if (child != null)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Compares two file or directory paths for equality by normalizing them to their full absolute forms,
        /// trimming any trailing slashes or backslashes, and performing a case-insensitive comparison.
        /// </summary>
        private static bool ArePathsEqual(string path1, string path2)
                    => string.Equals(
                            Path.GetFullPath(path1).TrimEnd('\\', '/'),
                            Path.GetFullPath(path2).TrimEnd('\\', '/'),
                            StringComparison.OrdinalIgnoreCase);

        #endregion Private Methods
    }
}
