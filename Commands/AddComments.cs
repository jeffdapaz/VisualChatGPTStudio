using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AddComments)]
    internal sealed class AddComments : BaseChatGPTCommand<AddComments>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            if (selectedText.Contains(Environment.NewLine))
            {
                return CommandType.Replace;
            }

            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            if (selectedText.Contains(Environment.NewLine))
            {
                return $"{OptionsCommands.AddCommentsForLines}{Environment.NewLine}{Environment.NewLine}{selectedText}";
            }

            return $"{OptionsCommands.AddCommentsForLine}{Environment.NewLine}{Environment.NewLine}{selectedText}";
        }
    }
}
