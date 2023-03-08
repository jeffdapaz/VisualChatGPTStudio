using EnvDTE;
using JeffPires.VisualChatGPTStudio.Commands.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.FindBugs)]
    internal sealed class FindBugs : BaseChatGPTCommand<FindBugs>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"Find Bugs{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
