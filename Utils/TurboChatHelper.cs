using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// This static class provides helper methods for the TurboChat.
    /// </summary>
    public static class TurboChatHelper
    {
        public static List<ChatTurboResponseSegment> GetChatTurboResponseSegments(string response)
        {
            const string DIVIDER = "```";

            Regex regex = new($@"({DIVIDER}([\s\S]*?){DIVIDER})");

            MatchCollection matches = regex.Matches(response);

            List<ChatTurboResponseSegment> substrings = new();

            //Get all substrings from the separation with the character ```
            string[] allSubstrings = response.Split(new string[] { DIVIDER }, StringSplitOptions.None);

            int indexFirstLine;

            // Identify the initial and final position of each substring that appears between the characters ```
            foreach (Match match in matches)
            {
                int start = match.Index;
                int end = start + match.Length;

                indexFirstLine = match.Value.IndexOf('\n');

                substrings.Add(new ChatTurboResponseSegment
                {
                    IsCode = true,
                    Content = Environment.NewLine + match.Value.Substring(indexFirstLine + 1).Replace(DIVIDER, string.Empty) + Environment.NewLine,
                    Start = start,
                    End = end
                });
            }

            bool matched;

            // Identify the initial and final position of each substring that does not appear between special characters ``` 
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

                substrings.Add(new ChatTurboResponseSegment
                {
                    IsCode = false,
                    Content = allSubstrings[i].Trim(),
                    Start = start,
                    End = end
                });
            }

            // Order the list of substrings by their starting position.
            return substrings.OrderBy(s => s.Start).ToList();
        }
    }

    /// <summary>
    /// This class provides methods for segmenting a ChatTurbo response into its component parts.
    /// </summary>
    public class ChatTurboResponseSegment
    {
        public bool IsCode { get; set; }

        public string Content { get; set; }

        public int Start { get; set; }

        public int End { get; set; }
    }
}
