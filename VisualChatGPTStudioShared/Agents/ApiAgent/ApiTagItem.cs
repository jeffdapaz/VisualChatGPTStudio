namespace VisualChatGPTStudioShared.Agents.ApiAgent
{
    /// <summary>
    /// Represents a tag used in an API context.
    /// </summary>
    public class ApiTagItem
    {
        private int type;

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
        public ApiTagType Type
        {
            get
            {
                return (ApiTagType)type;
            }
            set
            {
                type = (int)value;
            }
        }

        /// <summary>
        /// Gets or sets the type as an integer.
        /// </summary>
        public int TypeAsInteger
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }
    }

    /// <summary>
    /// Represents the types of API tags, such as Header or QueryString, used for categorizing API parameters.
    /// </summary>
    public enum ApiTagType
    {
        Header = 0,
        QueryString = 1
    }
}