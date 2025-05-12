using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using JeffPires.VisualChatGPTStudio.Utils;

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
                return OptionsCommands.GetCommandAsync(CommandsType.AddCommentsForLines).Result;
            }

            return OptionsCommands.GetCommandAsync(CommandsType.AddCommentsForLine).Result;
        }

        private bool CodeContainsMultipleLines(string code)
        {
            return code.Contains("\r\n") || code.Contains("\n") || code.Contains("\r");
        }
    }
}
