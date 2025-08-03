using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAI_API.ResponsesAPI.Models.Request
{
    public class ComputerUseRequest
    {
        [JsonProperty("model")]
        public string Model => "computer-use-preview";

        [JsonProperty("tools")]
        public List<ComputerUseTool> Tools { get; set; }

        [JsonProperty("input")]
        public List<ComputerUseInput> Input { get; set; }

        [JsonProperty("reasoning")]
        public ComputerUseReasoning Reasoning { get; set; } = new ComputerUseReasoning("concise");

        [JsonProperty("truncation")]
        public string Truncation => "auto";

        [JsonProperty("previous_response_id")]
        public string PreviousResponseId { get; set; }
    }
}