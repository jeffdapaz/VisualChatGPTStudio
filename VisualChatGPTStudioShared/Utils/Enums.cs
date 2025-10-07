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
}
