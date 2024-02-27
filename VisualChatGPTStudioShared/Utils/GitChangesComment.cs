using Community.VisualStudio.Toolkit;using EnvDTE;using LibGit2Sharp;using Microsoft.VisualStudio.Shell;using System;using System.Text;namespace VisualChatGPTStudioShared.Utils{
    /// <summary>
    /// Provides functionality to comment on Git changes.
    /// </summary>
    public static class GitChangesComment    {
        /// <summary>
        /// Retrieves the current changes in the Git repository of the solution, including file changes and their statuses.
        /// </summary>
        public static string GetCurrentChanges()        {            string repositoryPath = GetSolutionGitRepositoryPath();            if (string.IsNullOrWhiteSpace(repositoryPath))            {                throw new ArgumentNullException(nameof(repositoryPath));            }            StringBuilder result = new();            using (Repository repo = new(repositoryPath))            {
                //Capture the differences between the HEAD and the working directory
                Patch changes = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree, DiffTargets.WorkingDirectory);                foreach (PatchEntryChanges change in changes)                {                    result.AppendLine($"File: {change.Path}");                    result.AppendLine($"Status: {change.Status}");                    result.AppendLine("Changes:");                    result.AppendLine(change.Patch);                    result.AppendLine();                }            }            return result.ToString();        }

        /// <summary>
        /// Retrieves the file system path of the Git repository that contains the currently opened solution in Visual Studio.
        /// </summary>
        /// <returns>
        /// The file system path of the Git repository containing the solution, or an empty string if the solution is not part of a Git repository.
        /// </returns>
        public static string GetSolutionGitRepositoryPath()        {            ThreadHelper.ThrowIfNotOnUIThread();            DTE dte = VS.GetServiceAsync<DTE, DTE>().Result;            System.IO.DirectoryInfo directoryInfo = new(System.IO.Path.GetDirectoryName(dte.Solution.FullName));            while (directoryInfo?.Parent != null)            {                if (System.IO.Directory.Exists(System.IO.Path.Combine(directoryInfo.FullName, ".git")))                {                    return directoryInfo.FullName;                }                directoryInfo = directoryInfo.Parent;            }            return string.Empty;        }    }}