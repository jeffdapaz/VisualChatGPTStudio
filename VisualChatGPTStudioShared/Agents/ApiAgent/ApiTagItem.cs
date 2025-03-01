namespace VisualChatGPTStudioShared.Agents.ApiAgent
{
    /// <summary>
    /// Represents a tag used in an API context.
    /// </summary>
    public class ApiTagItem
    {
        /// <summary>
        /// Gets or sets the tag key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the tag value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the type of the API tag, represented by the <see cref="ApiTagType"/> enumeration.
        /// </summary>
        public ApiTagType Type { get; set; }
    }

    /// <summary>
    /// Represents the types of API tags, such as Header or QueryString, used for categorizing API parameters.
    /// </summary>
    public enum ApiTagType
    {
        Header,
        QueryString
    }
}