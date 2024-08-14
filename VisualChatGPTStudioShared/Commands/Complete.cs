using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using JeffPires.VisualChatGPTStudio.Utils;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    [Command(PackageIds.Complete)]
    internal sealed class Complete : BaseGenericCommand<Complete>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return string.Empty;
            }

            return TextFormat.FormatForCompleteCommand(OptionsCommands.GetCommandAsync(CommandsType.Complete).Result + Utils.Constants.PROVIDE_ONLY_CODE_INSTRUCTION, docView.FilePath);
        }
    }
}
