namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Represents the different types of requests that can be used.
    /// </summary>
    enum RequestType
    {
        Code = 0,
        Request = 1
    }

    /// <summary>
    /// Enum to represent the different types of commands that can be used.
    /// </summary>
    enum CommandType
    {
        Replace,
        InsertBefore,
        InsertAfter
    }

    /// <summary>
    /// Specifies the available options for selecting a Copilot model.
    /// </summary>
    public enum CopilotModelOption
    {
        Default = 0,
        Completion = 1,
        Specific = 2
    }
}
