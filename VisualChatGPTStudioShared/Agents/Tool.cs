using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAI_API.Functions;

namespace JeffPires.VisualChatGPTStudio.Agents
{
    public class Tool
    {
        public Tool()
        {
        }

        public Tool(Func<Tool, IReadOnlyDictionary<string, object>, Task<ToolResult>> executeWithToolAsync)
        {
            ExecuteAsync = args => executeWithToolAsync(this, args);
        }

        /// <summary>
        /// Name of tool
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Description for LLM
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Example in system message
        /// </summary>
        public string ExampleToSystemMessage { get; init; }

        /// <summary>
        /// Description for user for Approval request
        /// TODO
        /// </summary>
        public string ApprovalDescription { get; init; }

        /// <summary>
        /// Enabled for use
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Approval behavior
        /// </summary>
        public ApprovalKind Approval { get; init; } = ApprovalKind.Ask;

        /// <summary>
        /// Category for grouping tools in UI
        /// </summary>
        public string Category { get; init; } = "General";

        /// <summary>
        /// Risk level for security considerations
        /// </summary>
        public RiskLevel RiskLevel { get; init; } = RiskLevel.Medium;

        /// <summary>
        /// Function properties. Used for native tools_calling
        /// </summary>
        public object Properties { get; init; } = new Dictionary<string, Property>();

        /// <summary>
        /// Function to execute the tool
        /// </summary>
        public Func<IReadOnlyDictionary<string, object>, Task<ToolResult>> ExecuteAsync { get; init; } = null!;

        public bool LogResponseAndRequest { get; set; }
    }
}
