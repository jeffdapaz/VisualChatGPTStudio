using Newtonsoft.Json;

namespace OpenAI_API.ResponsesAPI.Models.Request
{
    public class ComputerUseSafetyCheck
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
