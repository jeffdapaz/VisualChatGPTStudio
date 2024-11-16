namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Contains constants used throughout the application.
    /// </summary>
    public static class Constants
    {
        public const string EDIT_DOCUMENT_COMMAND = "Edit.FormatDocument";

        public const string EXTENSION_NAME = "Visual chatGPT Studio";
        public const string EXTENSION_NAME_UNDERLINED = "Visual_chatGPT_Studio";
        public const string MESSAGE_SET_API_KEY = "Please, set the OpenAI API key.";
        public const string MESSAGE_WAITING_CHATGPT = "Visual chatGPT Studio - Waiting API response... (Alt+Z To Cancel)";
        public const string MESSAGE_WAITING_COPILOT = "Visual chatGPT Studio Copilot - Fetching suggestion...";
        public const string MESSAGE_WRITE_REQUEST = "Please write a request.";
        public const string MESSAGE_SET_COMMAND = "Please, set the command for \"{0}\" through the Options.";
        public const string PROVIDE_ONLY_CODE_INSTRUCTION = ". Please, only provide the code without additional comments or text.";
    }
}
