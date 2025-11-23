namespace JeffPires.VisualChatGPTStudio.Agents;

public enum RiskLevel
{
    /// <summary>
    /// Read-only operations
    /// </summary>
    Low,

    /// <summary>
    /// File operations, etc.
    /// </summary>
    Medium,

    /// <summary>
    /// System operations, execution
    /// </summary>
    High
}
