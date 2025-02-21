using Newtonsoft.Json;
using OpenAI_API.Chat;
using System.Collections.Generic;

namespace JeffPires.VisualChatGPTStudio.Utils.API
{
    /// <summary>
    /// Overrides the ChatRequest class to add the possibility to send tools for function requests
    /// </summary>
    public class ChatRequestOverride(ChatRequest basedOn) : ChatRequest(basedOn)
    {
        /// <summary>
        /// Get or set the tools for function requests
        /// </summary>
        [JsonProperty("tools")]
        public List<FunctionRequest> Tools { get; set; }
    }
}
