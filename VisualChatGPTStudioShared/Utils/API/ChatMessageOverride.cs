using Newtonsoft.Json;
using OpenAI_API.Chat;

namespace JeffPires.VisualChatGPTStudio.Utils.API
{
    /// <summary>
    /// Overrides the ChatMessage class to add the possibilty to pass the Content as object
    /// </summary>
    public class ChatMessageOverride : ChatMessage
    {
		/// <summary>
		/// Overrides the original content property.
		/// </summary>
		[JsonProperty("content")]
        public new object Content { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatMessageOverride"/> class with the specified role and content.
        /// </summary>
        /// <param name="role">The role of the chat message (e.g., user, assistant).</param>
        /// <param name="content">The content of the chat message, which can be of any object type.</param>
        public ChatMessageOverride(ChatMessageRole role, object content)
        {
            this.Role = role;
            this.Content = content;
        }
    }
}