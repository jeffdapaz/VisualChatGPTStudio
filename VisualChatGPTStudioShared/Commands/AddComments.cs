using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AddComments)]
    internal sealed class AddComments : BaseGenericCommand<AddComments>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            if (CodeContainsMultipleLines(selectedText))
            {
                return CommandType.Replace;
            }

            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            if (CodeContainsMultipleLines(selectedText))
            {
                return $"{OptionsCommands.AddCommentsForLines}{Environment.NewLine}{Environment.NewLine}{selectedText}";
            }

            return $"{OptionsCommands.AddCommentsForLine}{Environment.NewLine}{Environment.NewLine}{selectedText}";
        }

        private bool CodeContainsMultipleLines(string code)
        {
            return code.Contains("\r\n") || code.Contains("\n") || code.Contains("\r");
        }
    }
}
