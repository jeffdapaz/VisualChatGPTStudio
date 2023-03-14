using EnvDTE;
using JeffPires.VisualChatGPTStudio.Commands;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AddComments)]
    internal sealed class AddComments : BaseChatGPTCommand<AddComments>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            if (selectedText.Contains(Environment.NewLine))
            {
                return CommandType.Erase;
            }

            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            if (selectedText.Contains(Environment.NewLine))
            {
                return $"{OptionsCommands.AddCommentsForLines}{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
            }

            return $"{OptionsCommands.AddCommentsForLine}{Environment.NewLine}{Environment.NewLine}{TextFormat.FormatSelection(selectedText)}";
        }
    }
}
