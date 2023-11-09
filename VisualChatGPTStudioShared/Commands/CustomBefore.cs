using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options.Commands;

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
            return OptionsCommands.GetCommandAsync(CommandsType.CustomBefore).Result;
        }
    }
}
