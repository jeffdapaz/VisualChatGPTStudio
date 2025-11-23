using System.Text.Json.Serialization;

namespace JeffPires.VisualChatGPTStudio.Agents;

public record ToolResult
{
    /// <summary>
    /// Result showed to AI.
    /// </summary>
    public string Result { get; init; } = string.Empty;

    /// <summary>
    /// Optional. If not empty - private data to user, without sending to LLM.
    /// For example:
    /// - select data from database
    /// - call api and show result
    /// </summary>
    [JsonIgnore]
    public string PrivateResult { get; init; } = string.Empty;

    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Optional. Name of tool.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
