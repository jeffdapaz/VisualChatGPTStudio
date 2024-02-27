using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options.Commands;

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
            return OptionsCommands.GetCommandAsync(CommandsType.Optimize).Result + PROVIDE_ONLY_CODE_INSTRUCTION;
        }
    }
}
