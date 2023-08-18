using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.Optimize)]
    internal sealed class Optimize : BaseGenericCommand<Optimize>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.Replace;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.Optimize}{Environment.NewLine}{Environment.NewLine}{selectedText}";
        }
    }
}
