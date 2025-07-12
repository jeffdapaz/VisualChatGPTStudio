using JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Internal static utility class containing methods for text formatting. 
    /// </summary>
    internal static class TextFormat
    {
        private const string CODE_DIVIDER = "```";

        /// <summary>
        /// Minifies the given text by replacing multiple whitespace characters with a specified replacement string.
        /// </summary>
        /// <param name="textToMinify">The input text that needs to be minified.</param>
        /// <param name="replacement">The string to replace the whitespace characters with.</param>
        /// <returns>
        /// A minified version of the input text with whitespace replaced by the specified replacement string.
        /// </returns>
        public static string MinifyText(string textToMinify, string replacement)
        {
            return Regex.Replace(textToMinify, @"\s+", replacement);
        }

        /// <summary>
        /// Removes the specified characters from the given text.
        /// </summary>
        /// <param name="text">The text from which to remove the characters.</param>
        /// <param name="charsToRemove">The characters to remove from the text.</param>
        /// <returns>The text with the specified characters removed.</returns>
        public static string RemoveCharactersFromText(string text, params string[] charsToRemove)
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
            Dictionary<string, string> extensions = new()
            {
                { "cs", "//" },
                { "js", "//" },
                { "vb", "'" },
                { "sql", "--" },
                { "py", "#" },
                { "java", "//" },
                { "cpp", "//" },
                { "php", "//" },
                { "swift", "//" },
                { "rb", "#" },
                { "lua", "--" },
                { "go", "//" },
                { "ts", "//" },
                { "kt", "//" },
                { "rs", "//" },
                { "c", "//" },
                { "html", "<!--" },
                { "xml", "<!--" },
                { "yml", "#" },
                { "ini", ";" },
                { "md", "<!--" },
                { "json", "//" },
                { "txt", "//" },
                { "bat", "REM" },
                { "vbs", "'" },
                { "tex", "%" },
                { "asm", ";" },
                { "makefile", "#" },
                { "dockerfile", "#" },
            };

            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.').ToLower();

            if (extensions.ContainsKey(extension))
            {
                return extensions[extension];
            }

            return string.Empty;
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
                language = "For C#";
            }
            else if (extension.Equals("vb", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "For Visual Basic";
            }
            else if (extension.Equals("sql", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "For SQL Server";
            }
            else if (extension.Equals("js", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "For Java Script";
            }

            return $"{command} {language}, and do not repeat the same code that I send in your response.";
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

        /// <summary>
        /// Adjusts the code language identifier within a given code string by replacing specific language markers with standardized ones.
        /// </summary>
        /// <param name="code">The input code string containing language markers to be adjusted.</param>
        /// <returns>
        /// A string with the adjusted language markers.
        /// </returns>
        public static string AdjustCodeLanguage(string code)
        {
            string pattern = $"{CODE_DIVIDER}(csharp|c#|javascript)";

            return Regex.Replace(code, pattern, match =>
            {
                return match.Value switch
                {
                    var m when m.EndsWith("csharp", StringComparison.OrdinalIgnoreCase) => $"{CODE_DIVIDER}C",
                    var m when m.EndsWith("c#", StringComparison.OrdinalIgnoreCase) => $"{CODE_DIVIDER}C",
                    var m when m.EndsWith("javascript", StringComparison.OrdinalIgnoreCase) => $"{CODE_DIVIDER}java",
                    _ => match.Value
                };
            }, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Retrieves the segments of a chat turbo response by splitting the response using a specified divider.
        /// </summary>
        /// <param name="response">The chat turbo response.</param>
        /// <returns>A list of ChatTurboResponseSegment objects representing the segments of the response.</returns>
        public static List<ChatMessageSegment> GetChatTurboResponseSegments(string response)
        {
            Regex regex = new($@"({CODE_DIVIDER}([\s\S]*?){CODE_DIVIDER})");

            MatchCollection matches = regex.Matches(response);

            List<ChatMessageSegment> substrings = [];

            //Get all substrings from the separation with the character 
            string[] allSubstrings = response.Split(new string[] { CODE_DIVIDER }, StringSplitOptions.None);

            int indexFirstLine;

            // Identify the initial and final position of each substring that appears between the characters 
            foreach (Match match in matches)
            {
                int start = match.Index;
                int end = start + match.Length;

                indexFirstLine = match.Value.IndexOf('\n');

                // Remove the language identifier (e.g., "csharp") if it exists
                string codeContent = match.Value.Substring(indexFirstLine + 1);
                codeContent = RemoveLanguageIdentifier(codeContent).Replace(CODE_DIVIDER, string.Empty);

                substrings.Add(new ChatMessageSegment
                {
                    Author = IdentifierEnum.ChatGPTCode,
                    Content = RemoveBlankLinesFromResult(codeContent),
                    SegmentOrderStart = start,
                    SegmentOrderEnd = end
                });
            }

            bool matched;

            // Identify the initial and final position of each substring that does not appear between special characters 
            for (int i = 0; i < allSubstrings.Length; i++)
            {
                matched = false;

                foreach (Match match in matches)
                {
                    if (match.Value.Contains(allSubstrings[i]))
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched)
                {
                    continue;
                }

                int start = response.IndexOf(allSubstrings[i]);
                int end = start + allSubstrings[i].Length;

                substrings.Add(new ChatMessageSegment
                {
                    Author = IdentifierEnum.ChatGPT,
                    Content = RemoveBlankLinesFromResult(allSubstrings[i]),
                    SegmentOrderStart = start,
                    SegmentOrderEnd = end
                });
            }

            // Order the list of substrings by their starting position.
            return substrings.OrderBy(s => s.SegmentOrderStart).ToList();
        }

        /// <summary>
        /// Removes the language identifier from the code content if it exists.
        /// </summary>
        /// <param name="codeContent">The code content with a potential language identifier.</param>
        /// <returns>The code content without the language identifier.</returns>
        private static string RemoveLanguageIdentifier(string codeContent)
        {
            // Regex to match common language identifiers (e.g., "csharp", "javascript", etc.)
            Regex languageIdentifierRegex = new(@"```[^\s]+");

            return languageIdentifierRegex.Replace(codeContent, string.Empty).TrimStart();
        }

        /// <summary>
        /// Removes code tags from OpenAI responses.
        /// </summary>
        /// <param name="response">The original response from OpenAI.</param>
        /// <returns>A string with code tags removed or modified based on the input conditions.</returns>
        public static string RemoveCodeTagsFromOpenAIResponses(string response)
        {
            List<ChatMessageSegment> segments = GetChatTurboResponseSegments(response);

            if (!segments.Any(s => s.Author == IdentifierEnum.ChatGPTCode))
            {
                return NormalizeLineBreaks(RemoveLinesStartingWithCodeTags(response));
            }

            StringBuilder result = new();

            foreach (ChatMessageSegment segment in segments)
            {
                if (segment.Author == IdentifierEnum.ChatGPTCode)
                {
                    result.AppendLine(segment.Content);
                }
            }

            return NormalizeLineBreaks(RemoveBlankLinesFromResult(result.ToString()));
        }

        /// <summary>
        /// Normalizes the line breaks in the given text to the environment's newline format.
        /// </summary>
        /// <param name="text">The input string containing various types of line breaks.</param>
        /// <returns>
        /// A string with all line breaks replaced by the environment's newline format.
        /// </returns>
        public static string NormalizeLineBreaks(string text)
        {
            return Regex.Replace(text, @"\r\n|\r|\n", Environment.NewLine);
        }

        /// <summary>
        /// Removes blank lines from the given string result.
        /// </summary>
        /// <param name="result">The string from which blank lines should be removed.</param>
        /// <returns>A string with blank lines removed.</returns>
        public static string RemoveBlankLinesFromResult(string result)
        {
            return result.TrimPrefix("\r\n").TrimPrefix("\n\n").TrimPrefix("\n").TrimPrefix("\r").TrimSuffix("\r\n").TrimSuffix("\n\n").TrimSuffix("\n").TrimSuffix("\r");
        }

        /// <summary>
        /// Splits a given text into an array of strings, each representing a line. The method supports different line endings: "\r\n" (Windows), "\r" (old Mac), and "\n" (Unix/Linux).
        /// </summary>
        /// <param name="text">The text to be split into lines.</param>
        /// <returns>
        /// An array of strings, where each string is a line from the input text.
        /// </returns>
        public static string[] SplitTextByLine(string text)
        {
            return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }

        /// <summary>
        /// Removes lines from a given text that start with code tags
        /// </summary>
        /// <param name="text">Original text</param>
        /// <returns>Text without the code tags</returns>
        private static string RemoveLinesStartingWithCodeTags(string text)
        {
            string[] lines = SplitTextByLine(text);

            IEnumerable<string> filteredLines = lines.Where(line => !line.StartsWith("```"));

            string result = string.Join(Environment.NewLine, filteredLines);

            return RemoveBlankLinesFromResult(result);
        }
    }
}
