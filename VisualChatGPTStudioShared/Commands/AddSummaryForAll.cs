﻿using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    /// <summary>
    /// Command to add summary for the entire class.
    /// </summary>
    [Command(PackageIds.AddSummaryForAll)]
    internal sealed class AddSummaryForAll : BaseCommand<AddSummaryForAll>
    {
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
                if (!await ValidateAPIKeyAsync())
                {
                    return;
                }

                if (!System.IO.Path.GetExtension(docView.FilePath).TrimStart('.').Equals("cs", StringComparison.InvariantCultureIgnoreCase))
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, "This command is for C# code only.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                string command = await OptionsCommands.GetCommandAsync(CommandsType.AddSummary);

                if (string.IsNullOrWhiteSpace(command))
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, string.Format(Constants.MESSAGE_SET_COMMAND, nameof(AddSummary)), buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

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

                totalDeclarations = root.DescendantNodes().Count(d => CheckIfMemberTypeIsValid(d)) + 1;

                if (totalDeclarations == 0)
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, "Without declaration(s) to add summary.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                int memberIndex = 1;

                CancellationTokenSource = new CancellationTokenSource();

                await VS.StatusBar.ShowProgressAsync(PROGRESS_MESSAGE, memberIndex, totalDeclarations);

                foreach (SyntaxNode member in root.DescendantNodes().Where(d => CheckIfMemberTypeIsValid(d)))
                {
                    editedCode = await AddSummaryToClassMemberAsync(tree, member, textBuffer, editedCode);

                    memberIndex++;

                    await VS.StatusBar.ShowProgressAsync(PROGRESS_MESSAGE, memberIndex, totalDeclarations);
                }

                docView.TextView.TextBuffer.Replace(new Span(0, code.Length), editedCode);

                await FormatDocumentAsync();

                await VS.StatusBar.ShowProgressAsync("Finished", totalDeclarations, totalDeclarations);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, totalDeclarations, totalDeclarations);

                if (!string.IsNullOrWhiteSpace(originalCode))
                {
                    docView.TextView.TextBuffer.Replace(new Span(0, originalCode.Length), originalCode);
                }

                if (ex is not OperationCanceledException)
                {
                    Logger.Log(ex);

                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, ex.Message, Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                }
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
            string code;
            string summary;

            if (classMember is ClassDeclarationSyntax || classMember is InterfaceDeclarationSyntax)
            {
                int line = tree.GetLineSpan(classMember.Span).StartLinePosition.Line;

                code = textBuffer.CurrentSnapshot.GetLineFromLineNumber(line).GetText();
            }
            else
            {
                code = RemoveRegionTagsFromCode(classMember.ToFullString().Trim());
            }

            summary = await RequestAsync(code);

            summary = TextFormat.RemoveCodeTagsFromOpenAIResponses(true, summary);

            if (string.IsNullOrWhiteSpace(summary))
            {
                return editedCode;
            }

            return editedCode.Replace(code, summary + Environment.NewLine + code);
        }

        /// <summary>
        /// Requests a summary from ChatGPT for the given code.
        /// </summary>
        /// <param name="code">The code to summarize.</param>
        /// <returns>The summary of the code.</returns>
        private async Task<string> RequestAsync(string code)
        {
            string command = await OptionsCommands.GetCommandAsync(CommandsType.AddSummary);

            command = TextFormat.FormatCommandForSummary($"{command}\r\n\r\n{{0}}\r\n\r\n", code);

            string result = await ChatGPT.GetResponseAsync(OptionsGeneral, command, code, new string[] { "public", "private", "internal" }, CancellationTokenSource.Token);

            result = RemoveBlankLinesFromResult(result);

            if (result.Contains("{") || result.Contains("}"))
            {
                return string.Empty;
            }

            return result;
        }

        /// <summary>
        /// Checks if the given SyntaxNode is a valid member type.
        /// </summary>
        /// <param name="member">The SyntaxNode to check.</param>
        /// <returns>True if the SyntaxNode is a valid member type, false otherwise.</returns>
        private bool CheckIfMemberTypeIsValid(SyntaxNode member)
        {
            return member is ClassDeclarationSyntax ||
                   member is InterfaceDeclarationSyntax ||
                   member is StructDeclarationSyntax ||
                   member is ConstructorDeclarationSyntax ||
                   member is MethodDeclarationSyntax ||
                   member is PropertyDeclarationSyntax ||
                   member is EnumDeclarationSyntax ||
                   member is DelegateDeclarationSyntax ||
                   member is EventDeclarationSyntax;
        }

        /// <summary>
        /// Removes region tags from the given code.
        /// </summary>
        /// <param name="code">The code to remove region tags from.</param>
        /// <returns>The code with region tags removed.</returns>
        private string RemoveRegionTagsFromCode(string code)
        {
            string[] lines = code.Split('\r');

            List<string> newLines = new();

            foreach (string line in lines)
            {
                if (!line.Contains("#region") && !line.Contains("#endregion"))
                {
                    newLines.Add(line);
                }
            }

            return RemoveBlankLinesFromResult(string.Join("\r", newLines));
        }
    }
}

