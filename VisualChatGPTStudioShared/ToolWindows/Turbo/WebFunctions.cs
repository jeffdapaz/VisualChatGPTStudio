namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    public static class WebFunctions
    {
        private static string JsString(string s)
            => s.Replace(@"\", @"\\").Replace("`", @"\`").Replace("\r", "").Replace("\n", "\\n");

        public static string ClearChat
            => "clearChat();";

        public static string RenderMermaid
            => "renderMermaid();";

        public static string ScrollToLastResponse
            => "scrollToLastResponse();";

        public static string AddMsg(string content, bool scrollToBottom = true)
            => $"addMsg('user', `{JsString(content)}`, {scrollToBottom.ToString().ToLower()});";

        public static string UpdateLastGpt(string content, bool scrollToBottom = true)
            => $"updateLastGpt(`{JsString(content)}`, {scrollToBottom.ToString().ToLower()});";

        public static string ReloadThemeCss(bool isDarkTheme)
            => $"reloadThemeCss({isDarkTheme.ToString().ToLower()});";
    }
}
