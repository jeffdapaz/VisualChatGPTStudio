using Newtonsoft.Json;

namespace OpenAI_API.ResponsesAPI.Models.Request
{
    public class ComputerUseReasoning
    {
        [JsonProperty("summary")]
        public string Summary { get; set; }

        public ComputerUseReasoning()
        {

        }

        public ComputerUseReasoning(string summary)
        {
            Summary = summary;
        }
    }
}
