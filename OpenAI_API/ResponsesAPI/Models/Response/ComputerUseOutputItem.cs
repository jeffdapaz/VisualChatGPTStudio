using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenAI_API.ResponsesAPI.Models.Request;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OpenAI_API.ResponsesAPI.Models.Response
{
    public class ComputerUseOutputItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ComputerUseOutputItemType Type { get; set; }

        [JsonProperty("call_id")]
        public string CallId { get; set; }

        [JsonProperty("content")]
        public List<ComputerUseContent> Content { get; set; }

        [JsonProperty("summary")]
        public List<ComputerUseContent> Summary { get; set; }

        [JsonProperty("action")]
        public ComputerUseAction Action { get; set; }

        [JsonProperty("pending_safety_checks")]
        public List<ComputerUseSafetyCheck> PendingSafetyChecks { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ComputerUseOutputItemType
    {
        [EnumMember(Value = "message")]
        message,
        [EnumMember(Value = "computer_call")]
        computer_call,
        [EnumMember(Value = "reasoning")]
        reasoning
    }
}
