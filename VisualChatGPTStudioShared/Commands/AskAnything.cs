using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;

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
            return selectedText;
        }
    }
}
