using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.CustomBefore)]
    internal sealed class CustomBefore : BaseGenericCommand<CustomBefore>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.CustomBefore}{Environment.NewLine}{Environment.NewLine}{selectedText}";
        }
    }
}
