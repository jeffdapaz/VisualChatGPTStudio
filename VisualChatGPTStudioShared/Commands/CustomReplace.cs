using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.CustomReplace)]
    internal sealed class CustomReplace : BaseGenericCommand<CustomReplace>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.Replace;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.CustomReplace}{Environment.NewLine}{Environment.NewLine}{selectedText}";
        }
    }
}
