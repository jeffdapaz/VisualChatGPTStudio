using System.Text.RegularExpressions;
using System.Web;

namespace JeffPires.VisualChatGPTStudio
{
    /// <summary>
    /// Internal static utility class containing methods for text formatting. 
    /// </summary>
    internal static class TextFormat
    {
        /// <summary>
        /// Formats the selection.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>Text selected formated</returns>
        public static string FormatSelection(string text)
        {
            text = HttpUtility.HtmlDecode(text);

            text = RemoveHtmlTags(text, "<span", "</span>");
            text = RemoveHtmlTags(text, "<!--", "-->");
            text = RemoveHtmlTags(text, "<script", "</script>");
            text = RemoveHtmlTags(text, "<style", "</style>");
            text = RemoveHtmlTags(text, "<summary", "</summary>");
            text = RemoveHtmlTags(text, "<param", "</param>");
            text = RemoveHtmlTags(text, "<returns", "</returns>");

            text = text.Replace("//", "").Replace("///", "");
            //
            //replace matches of these regexes with space
            return new Regex(@"<[^>]+>|&nbsp;", RegexOptions.Multiline | RegexOptions.Compiled).Replace(text, " ");
        }

        /// <summary>
        /// Formats a given command for a given language 
        /// for example for c#, for visual basic, for sql server, for java script
        /// or by default.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="selectedText">The selected text.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>A Formatted string</returns>
        public static string FormatForCompleteCommand(string command, string selectedText, string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.');

            string language = string.Empty;

            if (extension == "cs")
            {
                language = "for C#";
            }
            else if (extension == "vb")
            {
                language = "for Visual Basic";
            }
            else if (extension == "sql")
            {
                language = "for SQL Server";
            }
            else if (extension == "js")
            {
                language = "for Java Script";
            }

            return $"{command} {language}: {selectedText}";
        }

        /// <summary>
        /// Formats a command for a summary.
        /// </summary>
        /// <param name="command">The command to format.</param>
        /// <param name="selectedText">The selected text.</param>
        /// <returns>The formatted command.</returns>
        public static string FormatCommandForSummary(string command, string selectedText)
        {
            string summaryFormat;

            //Is not a function
            if (!(selectedText.Contains("(") && selectedText.Contains(")") && selectedText.Contains("{") && selectedText.Contains("}")))
            {
                summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>";
            }
            else if (selectedText.Contains(" void "))
            {
                if (selectedText.Contains("()"))
                {
                    summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>";
                }
                else
                {
                    summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>\r\n/// <param name=\"\"></param>";
                }
            }
            else
            {
                if (selectedText.Contains("()"))
                {
                    summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>\r\n/// <returns>\r\n/// \r\n/// </returns>";
                }
                else
                {
                    summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>\r\n/// <param name=\"\"></param>\r\n/// <returns>\r\n/// \r\n/// </returns>";
                }
            }

            return string.Format(command, summaryFormat) + Environment.NewLine + Environment.NewLine + FormatSelection(selectedText);
        }

        /// <summary>
        /// Removes the HTML tags.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <param name="startTag">The start tag.</param>
        /// <param name="endTag">The end tag.</param>
        /// <returns>Text without tags</returns>
        private static string RemoveHtmlTags(string html, string startTag, string endTag)
        {
            bool again;

            do
            {
                again = false;

                int startTagPos = html.IndexOf(startTag, 0, StringComparison.CurrentCultureIgnoreCase);

                if (startTagPos < 0)
                {
                    continue;
                }

                int endTagPos = html.IndexOf(endTag, startTagPos + 1, StringComparison.CurrentCultureIgnoreCase);

                if (endTagPos <= startTagPos)
                {
                    continue;
                }

                html = html.Remove(startTagPos, endTagPos - startTagPos + endTag.Length);

                again = true;

            } while (again);

            return html;
        }
    }
}
