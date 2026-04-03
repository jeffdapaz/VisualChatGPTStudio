namespace VisualChatGPTStudioShared.Agents.McpAgent
{
    /// <summary>
    /// Represents an MCP server definition configured by the user.
    /// </summary>
    public class McpServerItem
    {
        #region Constants

        #endregion Constants

        #region Properties

        private int transportType;

        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the MCP server.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the transport type.
        /// </summary>
        public McpTransportType TransportType
        {
            get
            {
                return (McpTransportType)transportType;
            }
            set
            {
                transportType = (int)value;
            }
        }

        /// <summary>
        /// Gets or sets the transport type as integer for persistence.
        /// </summary>
        public int TransportTypeAsInteger
        {
            get
            {
                return transportType;
            }
            set
            {
                transportType = value;
            }
        }

        /// <summary>
        /// Gets or sets the executable command for stdio transport.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the command arguments for stdio transport.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Gets or sets the working directory for stdio transport.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the endpoint URL for HTTP/SSE transport.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the environment variables in JSON format for stdio transport.
        /// </summary>
        public string EnvironmentVariablesJson { get; set; }

        /// <summary>
        /// Gets or sets whether the MCP server is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets a short description to be shown on selection controls.
        /// </summary>
        public string Description
        {
            get
            {
                if (TransportType == McpTransportType.Stdio)
                {
                    return $"{Name} (stdio): {Command}";
                }

                return $"{Name} (sse): {Endpoint}";
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="McpServerItem"/> class.
        /// </summary>
        public McpServerItem()
        {
            Enabled = true;
            TransportType = McpTransportType.Stdio;
        }

        #endregion Constructors

        #region Public Methods

        #endregion Public Methods

        #region Private Methods

        #endregion Private Methods
    }

    /// <summary>
    /// Defines supported MCP transport types.
    /// </summary>
    public enum McpTransportType
    {
        Stdio = 0,
        Sse = 1
    }
}
