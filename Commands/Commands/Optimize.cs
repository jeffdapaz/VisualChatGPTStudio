
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Commands.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.Optimize)]
    internal sealed class Optimize : BaseChatGPTCommand<Optimize>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.Erase;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"Optimize{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
