using System.Windows.Controls;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    internal class ChatItem
    {
        public string Id { get; set; }

        public string Name { get; set; }        

        public ucChatHeader Header { get; set; }

        public TabItem TabItem { get; set; }

        public ucChatItem ListItem { get; set; }
    }
}
