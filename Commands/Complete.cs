using Community.VisualStudio.Toolkit;

namespace JeffPires.VisualChatGPTStudio.Commands
{
    [Command(PackageIds.Complete)]
    internal sealed class Complete : BaseChatGPTCommand<Complete>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return TextFormat.FormatForCompleteCommand(OptionsCommands.Complete, selectedText, docView.FilePath);
        }
    }
}
