using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options.Commands;

namespace VisualChatGPTStudioShared.Commands
{
    [Command(PackageIds.Translate)]
    internal sealed class Translate : BaseGenericCommand<Translate>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.Replace;
        }

        protected override string GetCommand(string selectedText)
        {
            return OptionsCommands.GetCommandAsync(CommandsType.Translate).Result;
        }
    }
}
