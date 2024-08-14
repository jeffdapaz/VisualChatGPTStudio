using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AddTests)]
    internal sealed class AddTests : BaseGenericCommand<AddTests>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.GetCommandAsync(CommandsType.AddTests).Result + Utils.Constants.PROVIDE_ONLY_CODE_INSTRUCTION;
        }
    }
}
