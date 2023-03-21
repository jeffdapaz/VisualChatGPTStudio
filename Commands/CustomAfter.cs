using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Utils;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.CustomAfter)]
    internal sealed class CustomAfter : BaseChatGPTCommand<CustomAfter>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.CustomAfter}{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
