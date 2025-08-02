using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAI_API.ResponsesAPI.Models.Request
{
    public class ComputerUseInput
    {
        [JsonProperty("role")]
        public string Role => "user";

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("content")]
        public List<ComputerUseContent> Content { get; set; }

        [JsonProperty("call_id")]
        public string CallId { get; set; }

        [JsonProperty("output")]
        public ComputerUseContent Output { get; set; }

        [JsonProperty("acknowledged_safety_checks")]
        public List<ComputerUseSafetyCheck> AcknowledgedSafetyChecks { get; set; }

    }
}