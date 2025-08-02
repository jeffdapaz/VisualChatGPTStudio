using Newtonsoft.Json;
using OpenAI_API.ResponsesAPI.Models.Request;
using System.Collections.Generic;

namespace OpenAI_API.ResponsesAPI.Models.Response
{
    public class ComputerUseOutputItem
    {
        [JsonProperty("type")]
        public ComputerUseOutputItemType Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("call_id")]
        public string CallId { get; set; }

        [JsonProperty("action")]
        public ComputerUseAction Action { get; set; }

        [JsonProperty("pending_safety_checks")]
        public List<ComputerUseSafetyCheck> PendingSafetyChecks { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        // Para reasoning
        [JsonProperty("summary")]
        public List<ComputerUseContent> Summary { get; set; }
    }

    public enum ComputerUseOutputItemType
    {
        computer_call,
        reasoning
    }
}
