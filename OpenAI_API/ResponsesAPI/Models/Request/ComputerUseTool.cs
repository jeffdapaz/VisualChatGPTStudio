using Newtonsoft.Json;

namespace OpenAI_API.ResponsesAPI.Models.Request
{
    public class ComputerUseTool
    {
        [JsonProperty("type")]
        public string Type => "computer_use_preview";

        [JsonProperty("display_width")]
        public int DisplayWidth { get; set; }

        [JsonProperty("display_height")]
        public int DisplayHeight { get; set; }

        [JsonProperty("environment")]
        public string Environment => "windows";
    }
}