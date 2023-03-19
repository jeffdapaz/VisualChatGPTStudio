using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Utils;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.Optimize)]
    internal sealed class Optimize : BaseChatGPTCommand<Optimize>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.Erase;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.Optimize}{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
