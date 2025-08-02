using Newtonsoft.Json;
using OpenAI_API.ResponsesAPI.Models.Request;
using System.Collections.Generic;

namespace OpenAI_API.ResponsesAPI.Models.Response
{
    public class ComputerUseResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("output")]
        public List<ComputerUseOutputItem> Output { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("reasoning")]
        public ComputerUseReasoning Reasoning { get; set; }

        [JsonProperty("previous_response_id")]
        public string PreviousResponseId { get; set; }
    }
}
