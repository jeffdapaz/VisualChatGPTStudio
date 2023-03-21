using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Utils;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.CustomBefore)]
    internal sealed class CustomBefore : BaseChatGPTCommand<CustomBefore>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.CustomBefore}{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
