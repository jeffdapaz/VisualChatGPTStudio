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
        /// Opens a diff view in Visual Studio to compare the contents of two code strings.
        /// </summary>
        /// <param name="filesExtension">The files extension.</param>
        /// <param name="originalCode">The code to use as the "original" file.</param>
        /// <param name="modifiedCode">The code to use as the "modified" file.</param>
        public static async System.Threading.Tasks.Task ShowDiffViewAsync(string filesExtension, string originalCode, string modifiedCode)
        {
            filesExtension = filesExtension.TrimStart('.');

            string tempFolder = System.IO.Path.GetTempPath();
            string tempFilePath1 = System.IO.Path.Combine(tempFolder, $"Original.{filesExtension}");
            string tempFilePath2 = System.IO.Path.Combine(tempFolder, $"Modified.{filesExtension}");

            System.IO.File.WriteAllText(tempFilePath1, originalCode);
            System.IO.File.WriteAllText(tempFilePath2, modifiedCode);

            DTE dte = await VS.GetServiceAsync<DTE, DTE>();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            dte.ExecuteCommand("Tools.DiffFiles", $"\"{tempFilePath1}\" \"{tempFilePath2}\"");
        }
    }
}
