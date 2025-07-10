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
        public const string PROVIDE_ONLY_CODE_INSTRUCTION = ". Your response must contain only the code required. Do not add any comments, explanations, or extra text—only code. The generated code must be valid, compilable, and consistent with the surrounding context provided in the prompt.";
        public const string COPILOT_ADDICTIONAL_INSTRUCTIONS = ". You are a programming assistant specialized in completing C# code snippets. Whenever the prompt contains the marker **AUTOCOMPLETE HERE**, you must generate only the code that fits precisely in that location, without including any part of the code that appears before or after the marker. Your response must contain only the code required to fill the space marked by **AUTOCOMPLETE HERE**. Do not include the **AUTOCOMPLETE HERE** marker in your response. Do not repeat any code that appears before or after the marker in the prompt. Do not add any comments, explanations, or extra text—only code. The generated code must be valid, compilable, and consistent with the surrounding context provided in the prompt. The marker may appear in various positions (inside a method, inside a block, etc.), so adapt your output accordingly. If you determine that the surrounding code already satisfies the logic and no additional code is needed, you may return an empty response.";
    }
}
