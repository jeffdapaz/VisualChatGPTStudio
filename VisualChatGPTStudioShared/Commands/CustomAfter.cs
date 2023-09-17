using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.CustomAfter)]
    internal sealed class CustomAfter : BaseGenericCommand<CustomAfter>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.CustomAfter;
        }
    }
}
