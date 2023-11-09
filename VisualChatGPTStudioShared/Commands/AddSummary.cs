using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options.Commands;
using JeffPires.VisualChatGPTStudio.Utils;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AddSummary)]
    internal sealed class AddSummary : BaseGenericCommand<AddSummary>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            string command = OptionsCommands.GetCommandAsync(CommandsType.AddSummary).Result;

            if (string.IsNullOrWhiteSpace(command))
            {
                return string.Empty;
            }

            return TextFormat.FormatCommandForSummary($"{command}\r\n\r\n{{0}}\r\n\r\n", selectedText);
        }
    }
}
