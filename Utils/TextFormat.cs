using System;
using System.Text.RegularExpressions;
using System.Web;

namespace JeffPires.VisualChatGPTStudio.Utils
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
        /// Detects the language of the given code.
        /// </summary>
        /// <param name="code">The code to detect the language of.</param>
        /// <returns>The language of the given code, or an empty string if the language could not be determined.</returns>
        public static string DetectCodeLanguage(string code)
        {
            Regex regex = new(@"(<\?xml.+?\?>)|(<.+?>.*?<\/.+?>)");

            if (regex.IsMatch(code))
            {
                return "XML";
            }

            regex = new(@"(<.+?>.*?<\/.+?>)");

            if (regex.IsMatch(code))
            {
                return "HTML";
            }

            regex = new(@"(public|private|protected|internal|static|class|void|string|double|float|in)");

            if (regex.IsMatch(code))
            {
                return "C#";
            }

            regex = new(@"(Public|Private|Protected|Friend|Static|Class|Sub|Function|End Sub|End Function|Dim|As|Integer|Boolean|String|Double|Single|If|Else|End If|While|End While|For|To|Step|Next|Each|In|Return)");

            if (regex.IsMatch(code))
            {
                return "VB";
            }

            regex = new(@"(function|do|switch|case|break|continue|let|instanceof|undefined|super|\bconsole\.)");

            if (regex.IsMatch(code))
            {
                return "JavaScript";
            }

            regex = new(@"([^{]*\{[^}]*\})");

            if (regex.IsMatch(code))
            {
                return "CSS";
            }

            regex = new(@"(SELECT|FROM|WHERE|JOIN|LEFT\s+JOIN|RIGHT\s+JOIN|INNER\s+JOIN|OUTER\s+JOIN|ON|GROUP\s+BY|HAVING|ORDER\s+BY|LIMIT|\bAND\b|\bOR\b|\bNOT\b|\bIN\b|\bBETWEEN\b|\bLIKE\b|\bIS\s+NULL\b|\bIS\s+NOT\s+NULL\b|\bEXISTS\b|\bCOUNT\b|\bSUM\b|\bAVG\b|\bMIN\b|\bMAX\b|\bCAST\b|\bCONVERT\b|\bDATEADD\b|\bDATEDIFF\b|\bDATENAME\b|\bDATEPART\b|\bGETDATE\b|\bYEAR\b|\bMONTH\b|\bDAY\b|\bHOUR\b|\bMINUTE\b|\bSECOND\b|\bTOP\b|\bDISTINCT\b|\bAS\b)");
            Regex regex2 = new(@"(select|from|where|join|left\s+join|right\s+join|inner\s+join|outer\s+join|on|group\s+by|having|order\s+by|limit|\band\b|\bor\b|\bnot\b|\bin\b|\bbetween\b|\blike\b|\bis\s+null\b|\bis\s+not\s+null\b|\bexists\b|\bcount\b|\bsum\b|\bavg\b|\bmin\b|\bmax\b|\bcast\b|\bconvert\b|\bdateadd\b|\bdatediff\b|\bdatename\b|\bdatepart\b|\bgetdate\b|\byear\b|\bmonth\b|\bday\b|\bhour\b|\bminute\b|\bsecond\b|\btop\b|\bdistinct\b|\bas\b)");

            if (regex.IsMatch(code) || regex2.IsMatch(code))
            {
                return "TSQL";
            }

            return string.Empty;
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
