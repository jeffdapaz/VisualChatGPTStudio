using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAI_API.ResponsesAPI.Models.Request
{
    public class ComputerUseInput
    {
        [JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
        public string Role { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public List<ComputerUseContent> Content { get; set; }

        [JsonProperty("call_id", NullValueHandling = NullValueHandling.Ignore)]
        public string CallId { get; set; }

        [JsonProperty("output", NullValueHandling = NullValueHandling.Ignore)]
        public ComputerUseContent Output { get; set; }

        [JsonProperty("acknowledged_safety_checks", NullValueHandling = NullValueHandling.Ignore)]
        public List<ComputerUseSafetyCheck> AcknowledgedSafetyChecks { get; set; }
    }
}