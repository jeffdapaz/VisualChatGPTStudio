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

        public static string AddMsg(IdentifierEnum user, string content)
            => $"addMsg('{user.ToString().ToLower()}', `{JsString(content)}`);";

        public static string UpdateLastGpt(string content)
            => $"updateLastGpt(`{JsString(content)}`);";

        public static string AddTable(string content)
            => $"addTable(`{JsString(content)}`);";

        public static string ReloadThemeCss(bool isDarkTheme)
            => $"reloadThemeCss({isDarkTheme.ToString().ToLower()});";
    }
}
