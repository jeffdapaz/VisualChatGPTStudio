using EnvDTE;
using JeffPires.VisualChatGPTStudio.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AddSummary)]
    internal sealed class AddSummary : BaseChatGPTCommand<AddSummary>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            return TextFormat.FormatCommandForSummary("Only write a comment as C# summary format like\r\n\r\n{0}\r\n\r\nfor this", selectedText);
        }
    }
}
