using System;
using System.Text.RegularExpressions;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Internal static utility class containing methods for text formatting. 
    /// </summary>
    internal static class TextFormat
    {
        /// <summary>
        /// Removes all whitespace and break lines from a given string.
        /// </summary>
        /// <param name="textToMinify">The string to minify.</param>
        /// <returns>A minified version of the given string.</returns>
        public static string MinifyText(string textToMinify)
        {
            return Regex.Replace(textToMinify, @"\s+", " ");
        }

        /// <summary>
        /// Removes the specified characters from the given text.
        /// </summary>
        /// <param name="text">The text from which to remove the characters.</param>
        /// <param name="charsToRemove">The characters to remove from the text.</param>
        /// <returns>The text with the specified characters removed.</returns>
        public static string RemoveCharactersFromText(string text, string[] charsToRemove)
        {
            foreach (string character in charsToRemove)
            {
                if (!string.IsNullOrEmpty(character))
                {
                    text = text.Replace(character, string.Empty);
                }
            }

            return text;
        }

        /// <summary>
        /// Gets the comment characters for a given file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The comment characters for the given file path.</returns>
        public static string GetCommentChars(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.');

            if (extension.Equals("cs", StringComparison.InvariantCultureIgnoreCase) || extension.Equals("js", StringComparison.InvariantCultureIgnoreCase))
            {
                return "//";
            }

            if (extension.Equals("vb", StringComparison.InvariantCultureIgnoreCase))
            {
                return "'";
            }

            if (extension.Equals("sql", StringComparison.InvariantCultureIgnoreCase))
            {
                return "--";
            }

            return "<!--";
        }

        /// <summary>
        /// Formats a given command for a given language 
        /// for example for c#, for visual basic, for sql server, for java script
        /// or by default.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>A Formatted string</returns>
        public static string FormatForCompleteCommand(string command, string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.');

            string language = string.Empty;

            if (extension.Equals("cs", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "for C#";
            }
            else if (extension.Equals("vb", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "for Visual Basic";
            }
            else if (extension.Equals("sql", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "for SQL Server";
            }
            else if (extension.Equals("js", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "for Java Script";
            }

            return $"{command} {language}: ";
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

            return string.Format(command, summaryFormat) + Environment.NewLine + "for";
        }

        /// <summary>
        /// Detects the language of the given code.
        /// </summary>
        /// <param name="code">The code to detect the language of.</param>
        /// <returns>The language of the given code, or an empty string if the language could not be determined.</returns>
        public static string DetectCodeLanguage(string code)
        {
            Regex regexXML = new(@"(<\?xml.+?\?>)|(<.+?>.*?<\/.+?>)");
            Regex regexHTML = new(@"(<.+?>.*?<\/.+?>)");
            Regex regexCSharp = new(@"(public|private|protected|internal|static|class|void|string|double|float|in)");
            Regex regexVB = new(@"(Public|Private|Protected|Friend|Static|Class|Sub|Function|End Sub|End Function|Dim|As|Integer|Boolean|String|Double|Single|If|Else|End If|While|End While|For|To|Step|Next|Each|In|Return)");
            Regex regexJS = new(@"(function|do|switch|case|break|continue|let|instanceof|undefined|super|\bconsole\.)");
            Regex regexCSS = new(@"([^{]*\{[^}]*\})");
            Regex regexTSQL1 = new(@"(CREATE|UPDATE|DELETE|INSERT|DROP|SELECT|FROM|WHERE|JOIN|LEFT\s+JOIN|RIGHT\s+JOIN|INNER\s+JOIN|OUTER\s+JOIN|ON|GROUP\s+BY|HAVING|ORDER\s+BY|LIMIT|\bAND\b|\bOR\b|\bNOT\b|\bIN\b|\bBETWEEN\b|\bLIKE\b|\bIS\s+NULL\b|\bIS\s+NOT\s+NULL\b|\bEXISTS\b|\bCOUNT\b|\bSUM\b|\bAVG\b|\bMIN\b|\bMAX\b|\bCAST\b|\bCONVERT\b|\bDATEADD\b|\bDATEDIFF\b|\bDATENAME\b|\bDATEPART\b|\bGETDATE\b|\bYEAR\b|\bMONTH\b|\bDAY\b|\bHOUR\b|\bMINUTE\b|\bSECOND\b|\bTOP\b|\bDISTINCT\b|\bAS\b)");
            Regex regexTSQL2 = new(@"(create|update|delete|insert|drop|select|from|where|join|left\s+join|right\s+join|inner\s+join|outer\s+join|on|group\s+by|having|order\s+by|limit|\band\b|\bor\b|\bnot\b|\bin\b|\bbetween\b|\blike\b|\bis\s+null\b|\bis\s+not\s+null\b|\bexists\b|\bcount\b|\bsum\b|\bavg\b|\bmin\b|\bmax\b|\bcast\b|\bconvert\b|\bdateadd\b|\bdatediff\b|\bdatename\b|\bdatepart\b|\bgetdate\b|\byear\b|\bmonth\b|\bday\b|\bhour\b|\bminute\b|\bsecond\b|\btop\b|\bdistinct\b|\bas\b)");

            if (regexXML.IsMatch(code))
            {
                return "XML";
            }

            if (regexHTML.IsMatch(code))
            {
                return "HTML";
            }

            if (regexCSharp.IsMatch(code))
            {
                return "C#";
            }

            if (regexVB.IsMatch(code))
            {
                return "VB";
            }

            if (regexJS.IsMatch(code))
            {
                return "JavaScript";
            }

            if (regexCSS.IsMatch(code))
            {
                return "CSS";
            }

            if (regexTSQL1.IsMatch(code) || regexTSQL2.IsMatch(code))
            {
                return "TSQL";
            }

            return string.Empty;
        }
    }
}
