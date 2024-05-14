﻿using Community.VisualStudio.Toolkit;
    /// <summary>
    /// Provides functionality to Git changes.
    /// </summary>
    public static class GitChanges
        /// <summary>
        /// Retrieves the current changes in the Git repository of the solution, including file changes and their statuses.
        /// </summary>
        public static string GetCurrentChangesAsString()
                //Capture the differences between the HEAD and the working directory
                Patch changes = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree, DiffTargets.WorkingDirectory);

        /// <summary>
        /// Retrieves the current changes in the Git repository of the solution as a patch.
        /// </summary>
        /// <returns>A Patch object representing the current changes.</returns>
        public static Patch GetCurrentChanges()
                //Capture the differences between the HEAD and the working directory
                return repo.Diff.Compare<Patch>(repo.Head.Tip.Tree, DiffTargets.WorkingDirectory);

        /// <summary>
        /// Separates the given git changes into original and altered code.
        /// </summary>
        /// <param name="gitChanges">The string containing git changes.</param>
        /// <param name="originalCode">The output string containing the original code.</param>
        /// <param name="alteredCode">The output string containing the altered code.</param>
        public static void SeparateCodeChanges(string gitChanges, out string originalCode, out string alteredCode)
        {
            originalCode = string.Empty;
            alteredCode = string.Empty;

            //Split the input text into lines
            string[] lines = gitChanges.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                // Filters out rows that should not be included in the final result
                if (line.StartsWith("---") || line.StartsWith("+++") || line.Contains("diff --git") || line.StartsWith("@@") || line.StartsWith("index "))
                {
                    continue;
                }

                // Check if the line is original code (-)
                if (line.StartsWith("-"))
                {
                    originalCode += line.Substring(1) + Environment.NewLine; // Remove the prefix '-' and add it to the original code string.
                }
                // Check if the line is of modified code (+)
                else if (line.StartsWith("+"))
                {
                    alteredCode += line.Substring(1) + Environment.NewLine; // Remove the '+' prefix and add it to the modified code string
                }
                // For unchanged lines
                else
                {
                    originalCode += line + Environment.NewLine; // Add the line to the original code
                    alteredCode += line + Environment.NewLine; // Add the line to the modified code
                }
            }
        }

        /// <summary>
        /// Retrieves the file system path of the Git repository that contains the currently opened solution in Visual Studio.
        /// </summary>
        /// <returns>
        /// The file system path of the Git repository containing the solution, or an empty string if the solution is not part of a Git repository.
        /// </returns>
        public static string GetSolutionGitRepositoryPath()