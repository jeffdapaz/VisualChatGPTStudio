using System.Collections.Generic;

namespace VisualChatGPTStudioShared.Agents.ApiAgent
{
    /// <summary>
    /// Represents an API definition.
    /// </summary>
    public class ApiItem
    {
        /// <summary>
        /// API definition ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the API's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the API's base URL.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// If true, all responses will be send to AI, when false, only HTTP status will be send.
        /// </summary>
        public bool SendResponsesToAI { get; set; }

        /// <summary>
        /// Gets or sets the collection of API tag items.
        /// </summary>
        public List<ApiTagItem> Tags { get; set; }

        /// <summary>
        /// Gets or sets the API's definition.
        /// </summary>
        public string Definition { get; set; }
    }
}