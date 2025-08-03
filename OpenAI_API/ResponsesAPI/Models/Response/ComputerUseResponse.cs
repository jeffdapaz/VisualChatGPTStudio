using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAI_API.ResponsesAPI.Models.Response
{
    public class ComputerUseResponse
    {
        [JsonProperty("output")]
        public List<ComputerUseOutputItem> Output { get; set; }
    }
}