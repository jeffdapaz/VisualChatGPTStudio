using EnvDTE;

namespace JeffPires.VisualChatGPTStudio.Commands.Commands
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
            return TextFormat.FormatForCompleteCommand("Please complete", selectedText, docView.FilePath);
        }
    }
}
