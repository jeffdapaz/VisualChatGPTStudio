using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace VisualChatGPTStudioShared.Utils
{
    /// <summary>
    /// Provides functionality to compare and display differences between two versions of code.
    /// </summary>
    public static class DiffView
    {
        /// <summary>
        /// Shows a diff view of two strings of code.
        /// </summary>
        /// <param name="originalCode">The original code.</param>
        /// <param name="optimizedCode">The optimized code.</param>
        public static async System.Threading.Tasks.Task ShowDiffViewAsync(string filePath, string originalCode, string optimizedCode)
        {
            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.');

            string tempFolder = System.IO.Path.GetTempPath();
            string tempFilePath1 = System.IO.Path.Combine(tempFolder, $"Original.{extension}");
            string tempFilePath2 = System.IO.Path.Combine(tempFolder, $"Optimized.{extension}");

            System.IO.File.WriteAllText(tempFilePath1, originalCode);
            System.IO.File.WriteAllText(tempFilePath2, optimizedCode);

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            dte.ExecuteCommand("Tools.DiffFiles", $"\"{tempFilePath1}\" \"{tempFilePath2}\"");
        }
    }
}
