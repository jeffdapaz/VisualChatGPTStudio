using EnvDTE;
using JeffPires.VisualChatGPTStudio.Commands.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.Explain)]
    internal sealed class Explain : BaseChatGPTCommand<Explain>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"Explain{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
