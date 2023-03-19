using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Utils;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.Explain)]
    internal sealed class Explain : BaseChatGPTCommand<Explain>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.Explain}{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
