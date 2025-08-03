using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace OpenAI_API.ResponsesAPI.Models.Response
{
    public class ComputerUseAction
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ComputerUseActionType Type { get; set; }

        [JsonProperty("button")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ComputerUseButton Button { get; set; }

        [JsonProperty("x")]
        public int? X { get; set; }

        [JsonProperty("y")]
        public int? Y { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum ComputerUseActionType
    {
        [EnumMember(Value = "click")]
        Click,
        [EnumMember(Value = "double_click")]
        DoubleClick,
        [EnumMember(Value = "scroll")]
        Scroll,
        [EnumMember(Value = "keypress")]
        KeyPress,
        [EnumMember(Value = "type")]
        Type,
        [EnumMember(Value = "wait")]
        Wait,
        [EnumMember(Value = "screenshot")]
        Screenshot,
        [EnumMember(Value = "unknown")]
        Unknown
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ComputerUseButton
    {
        [EnumMember(Value = "left")]
        Left,
        [EnumMember(Value = "right")]
        Right,
        [EnumMember(Value = "middle")]
        Middle,
        [EnumMember(Value = "unknown")]
        Unknown
    }
}
