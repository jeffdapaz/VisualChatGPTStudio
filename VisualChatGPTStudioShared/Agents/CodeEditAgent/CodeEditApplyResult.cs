using Newtonsoft.Json;

namespace VisualChatGPTStudioShared.Agents.CodeEditAgent
{
    /// <summary>
    /// Represents the result of applying an edit code operation.
    /// </summary>
    public class EditCodeApplyResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the message content associated with this edit. 
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the index of the failed edit operation, if any.
        [JsonProperty("failedEditIndex", NullValueHandling = NullValueHandling.Ignore)]
        public int? FailedEditIndex { get; set; }

        /// <summary>
        /// Gets or sets the updated code.
        /// </summary>
        [JsonProperty("updatedCode", NullValueHandling = NullValueHandling.Ignore)]
        public string UpdatedCode { get; set; }
    }
}