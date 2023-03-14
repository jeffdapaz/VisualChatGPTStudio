using EnvDTE;
using JeffPires.VisualChatGPTStudio.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AddTests)]
    internal sealed class AddTests : BaseChatGPTCommand<AddTests>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertAfter;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"{OptionsCommands.AddTests}{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
