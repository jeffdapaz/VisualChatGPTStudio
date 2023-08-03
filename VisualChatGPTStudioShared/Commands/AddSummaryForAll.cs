using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using OpenAI_API.Completions;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;

namespace VisualChatGPTStudioShared.Commands
{
    /// <summary>
    /// Command to add summary for the entire class.
    /// </summary>
    [Command(PackageIds.AddSummaryForAll)]
    internal sealed class AddSummaryForAll : BaseCommand<AddSummaryForAll>
    {
        /// <summary>
        /// Gets the OptionsGeneral property of the VisualChatGPTStudioPackage.
        /// </summary>
        protected OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).OptionsGeneral;
            }
        }

        /// <summary>
        /// Gets the OptionsCommands property of the VisualChatGPTStudioPackage.
        /// </summary>
        protected OptionPageGridCommands OptionsCommands
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).OptionsCommands;
            }
        }

        /// <summary>
        /// Executes the command to add summaries to class members.
        /// </summary>
        /// <param name="e">The <see cref="OleMenuCmdEventArgs"/> instance containing the event data.</param>
        protected override async System.Threading.Tasks.Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            const string PROGRESS_MESSAGE = "Creating Summaries...";

            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();

            string originalCode = string.Empty;

            int totalDeclarations = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(OptionsGeneral.ApiKey))
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, Constants.MESSAGE_SET_API_KEY, buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    Package.ShowOptionPage(typeof(OptionPageGridGeneral));

                    return;
                }

                if (!System.IO.Path.GetExtension(docView.FilePath).TrimStart('.').Equals("cs", StringComparison.InvariantCultureIgnoreCase))
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, "This command is for C# code only.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

                ITextBuffer textBuffer = docView.TextView.TextBuffer;

                originalCode = textBuffer.CurrentSnapshot.GetText();

                string code = originalCode;

                string editedCode = RemoveCurrentSummaries(code);

                docView.TextView.TextBuffer.Replace(new Span(0, code.Length), editedCode);

                code = textBuffer.CurrentSnapshot.GetText();

                editedCode = code;

                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);

                SyntaxNode root = tree.GetRoot();

                totalDeclarations = root.DescendantNodes().Count(d => d is ClassDeclarationSyntax || d is MethodDeclarationSyntax || d is PropertyDeclarationSyntax || d is EnumDeclarationSyntax) + 1;

                if (totalDeclarations == 0)
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, "Without declaration(s) to add summary.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                int memberIndex = 1;

                await VS.StatusBar.ShowProgressAsync(PROGRESS_MESSAGE, memberIndex, totalDeclarations);

                foreach (SyntaxNode member in root.DescendantNodes())
                {
                    if (member is ClassDeclarationSyntax || member is MethodDeclarationSyntax || member is PropertyDeclarationSyntax || member is EnumDeclarationSyntax)
                    {
                        editedCode = await AddSummaryToClassMemberAsync(tree, member, textBuffer, editedCode);

                        memberIndex++;

                        await VS.StatusBar.ShowProgressAsync(PROGRESS_MESSAGE, memberIndex, totalDeclarations);
                    }
                }

                docView.TextView.TextBuffer.Replace(new Span(0, code.Length), editedCode);

                try
                {
                    (await VS.GetServiceAsync<DTE, DTE>()).ExecuteCommand(Constants.EDIT_DOCUMENT_COMMAND, string.Empty);
                }
                catch (Exception)
                {

                }

                await VS.StatusBar.ShowProgressAsync("Finished", totalDeclarations, totalDeclarations);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(originalCode))
                {
                    docView.TextView.TextBuffer.Replace(new Span(0, originalCode.Length), originalCode);
                }

                await VS.StatusBar.ShowProgressAsync(ex.Message, totalDeclarations, totalDeclarations);

                await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, ex.Message, Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
            }
        }

        /// <summary>
        /// Removes all summary comments from the given class code.
        /// </summary>
        /// <param name="classCode">The class code to remove summaries from.</param>
        /// <returns>The class code with all summaries removed.</returns>
        public static string RemoveCurrentSummaries(string classCode)
        {
            StringBuilder outputBuilder = new();

            bool insideSummary = false;

            string startPattern = @"^\s*///\s*<summary\b[^>]*>";

            string[] lines = classCode.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (insideSummary)
                {
                    if (!lines[i + 1].Contains("///"))
                    {
                        insideSummary = false;
                        continue;
                    }
                }
                else if (Regex.IsMatch(lines[i], startPattern))
                {
                    insideSummary = true;
                    continue;
                }

                if (!insideSummary)
                {
                    outputBuilder.Append(lines[i]);
                }
            }

            return outputBuilder.ToString().Trim();
        }


        /// <summary>
        /// Adds the summary to the class member.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="classMember">The class member.</param>
        /// <param name="textBuffer">The text buffer.</param>
        /// <param name="editedCode">The edited code.</param>
        /// <returns>The code edited with the summary added.</returns>
        private async System.Threading.Tasks.Task<string> AddSummaryToClassMemberAsync(SyntaxTree tree, SyntaxNode classMember, ITextBuffer textBuffer, string editedCode)
        {
            int lineNumber;
            string declarationCode;
            string bodyCode;

            lineNumber = tree.GetLineSpan(classMember.Span).StartLinePosition.Line;

            declarationCode = textBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber).GetText();

            bodyCode = classMember.ToFullString().Trim();

            string summary = string.Empty;

            if (classMember is ClassDeclarationSyntax)
            {
                summary = await RequestAsync(declarationCode);
            }
            else
            {
                summary = await RequestAsync(bodyCode);
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                return string.Empty;
            }

            return editedCode.Replace(declarationCode, summary + Environment.NewLine + declarationCode);
        }

        /// <summary>
        /// Requests a summary from ChatGPT for the given code.
        /// </summary>
        /// <param name="code">The code to summarize.</param>
        /// <returns>The summary of the code.</returns>
        private async Task<string> RequestAsync(string code)
        {
            string command = TextFormat.FormatCommandForSummary($"{OptionsCommands.AddSummary}\r\n\r\n{{0}}\r\n\r\n", code);

            CompletionResult result = await ChatGPT.RequestAsync(OptionsGeneral, command, new[] { "public", "private", "internal" });

            string resultText = result.ToString();

            //This code checks if the string "resultText" starts with "\r\n" and if it does, it removes from the string. 
            //It will continue to do this until the string no longer starts with "\r\n". 
            while (resultText.StartsWith("\r\n"))
            {
                resultText = resultText.Substring(4);
            }

            if (resultText.Contains("{") || resultText.Contains("}"))
            {
                return string.Empty;
            }

            return resultText;
        }
    }
}

