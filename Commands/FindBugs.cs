using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.FindBugs)]
    internal sealed class FindBugs : BaseChatGPTCommand<FindBugs>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.FindBugs}{Environment.NewLine}{Environment.NewLine}{selectedText}";
        }
    }
}
