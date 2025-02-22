using Newtonsoft.Json;
using OpenAI_API.Functions;
using System.Collections.Generic;

namespace OpenAI_API.Chat
{
    /// <summary>
    /// Chat message sent or received from the API. Includes who is speaking in the "role" and the message text in the "content"
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Creates an empty <see cref="ChatMessage"/>, with <see cref="Role"/> defaulting to <see cref="ChatMessageRole.User"/>
        /// </summary>
        public ChatMessage()
        {
            this.Role = ChatMessageRole.User;
        }

        /// <summary>
        /// Initializes a new instance of the ChatMessage class with the specified role and content.
        /// </summary>
        /// <param name="role">The role associated with the chat message (e.g., user, system, assistant).</param>
        /// <param name="content">The content of the chat message.</param>
        public ChatMessage(ChatMessageRole role, object content)
        {
            this.Role = role;
            this.Content = content;
        }

        /// <summary>
        /// Initializes a new instance of the ChatMessage class with the specified role, content, and function ID.
        /// </summary>
        /// <param name="role">The role associated with the chat message.</param>
        /// <param name="content">The content of the chat message.</param>
        /// <param name="functionId">The identifier for the function associated with the chat message.</param>
        public ChatMessage(ChatMessageRole role, object content, string functionId)
        {
            this.Role = role;
            this.Content = content;
            this.FunctionId = functionId;
        }

        [JsonProperty("role")]
        internal string rawRole { get; set; }

        /// <summary>
        /// The role of the message, which can be "system", "assistant" or "user"
        /// </summary>
        [JsonIgnore]
        public ChatMessageRole Role
        {
            get
            {
                return ChatMessageRole.FromString(rawRole);
            }
            set
            {
                rawRole = value.ToString();
            }
        }

        /// <summary>
        /// The content of the message
        /// </summary>
        [JsonProperty("content")]
        public object Content { get; set; }

        /// <summary>
        /// An optional name of the user in a multi-user chat 
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Functions to be executed.
        /// </summary>
        [JsonProperty("tool_calls")]
        public IReadOnlyList<FunctionResult> Functions { get; set; }

        /// <summary>
        /// The function id.
        /// </summary>
        [JsonProperty("tool_call_id")]
        public string FunctionId { get; set; }
    }
}
