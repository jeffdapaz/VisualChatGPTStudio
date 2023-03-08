using EnvDTE;
using JeffPires.VisualChatGPTStudio.Commands.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AskAnything)]
    internal sealed class AskAnything : BaseChatGPTCommand<AskAnything>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return TextFormat.FormatSelection(selectedText);
        }
    }
}
