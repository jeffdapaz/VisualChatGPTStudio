using Newtonsoft.Json;

namespace OpenAI_API.ResponsesAPI.Models.Response
{
    public class ComputerUseAction
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("button")]
        public string Button { get; set; }

        [JsonProperty("x")]
        public int? X { get; set; }

        [JsonProperty("y")]
        public int? Y { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
