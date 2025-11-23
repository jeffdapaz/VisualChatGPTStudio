using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using JeffPires.VisualChatGPTStudio.Utils;

namespace JeffPires.VisualChatGPTStudio.Agents;

/// <summary>
/// LLM wanted to call tool.
/// </summary>
public class ToolToCall
{
    public Tool Tool { get; set; } = null!;

    public string ArgumentsJson { get; set; } = string.Empty;

    [JsonIgnore]
    public IReadOnlyDictionary<string, object> Parameters =>
        string.IsNullOrEmpty(ArgumentsJson)
            ? new Dictionary<string, object>()
            : JsonUtils.Deserialize<Dictionary<string, object>>(ArgumentsJson) ?? new Dictionary<string, object>();

    public bool IsApproved { get; set; }

    public bool IsProcessed { get; set; }

    public string CallId { get; set; } = Guid.NewGuid().ToString();

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public ToolResult? Result { get; set; }

    public void UpdateParameters(IReadOnlyDictionary<string, object> newParameters)
    {
        ArgumentsJson = JsonUtils.Serialize(newParameters);
    }
}
