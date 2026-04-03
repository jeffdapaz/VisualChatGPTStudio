using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenAI_API.Functions;
using VisualChatGPTStudioShared.Utils.Repositories;

namespace VisualChatGPTStudioShared.Agents.McpAgent
{
    /// <summary>
    /// Allows interaction with configured MCP servers through AI function calls.
    /// </summary>
    public static class McpAgent
    {
        #region Constants

        private const string FUNCTION_CALL_TOOL = nameof(CallTool);
        private const string FUNCTION_READ_RESOURCE = nameof(ReadResource);

        #endregion Constants

        #region Properties

        /// <summary>
        /// Represents the method that will handle an event when a function is being executed, providing a message as an argument.
        /// </summary>
        public delegate void OnExecutingFunctionHandler(string message);
        public static event OnExecutingFunctionHandler OnExecutingFunction;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes static resources for MCP agent persistence.
        /// </summary>
        static McpAgent()
        {
            McpAgentRepository.CreateDataBase();
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Retrieves all enabled MCP server definitions ordered by name.
        /// </summary>
        /// <returns>A list of enabled MCP servers.</returns>
        public static List<McpServerItem> GetServersDefinitions()
        {
            return McpAgentRepository
                .GetMcpServers()
                .Where(s => s.Enabled)
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// Returns MCP functions that the AI can call.
        /// </summary>
        /// <returns>List of <see cref="FunctionRequest"/>.</returns>
        public static List<FunctionRequest> GetFunctions()
        {
            List<FunctionRequest> functions = [];

            Parameter parameterCallTool = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    { "serverName", new Property { Types = ["string"], Description = "The configured MCP server name. Need to be the same indicated by the user." } },
                    { "toolName", new Property { Types = ["string"], Description = "The MCP tool name to execute." } },
                    { "argumentsJson", new Property { Types = ["string", "null"], Description = "Tool arguments in JSON object format. Use null when no arguments are needed." } }
                }
            };

            functions.Add(new FunctionRequest
            {
                Function = new Function
                {
                    Name = FUNCTION_CALL_TOOL,
                    Description = "Calls an MCP tool on a configured MCP server.",
                    Parameters = parameterCallTool
                }
            });

            Parameter parameterReadResource = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    { "serverName", new Property { Types = ["string"], Description = "The configured MCP server name. Need to be the same indicated by the user." } },
                    { "resourceUri", new Property { Types = ["string"], Description = "The resource URI to read, usually returned by ListResources." } }
                }
            };

            functions.Add(new FunctionRequest
            {
                Function = new Function
                {
                    Name = FUNCTION_READ_RESOURCE,
                    Description = "Reads one specific resource from a configured MCP server.",
                    Parameters = parameterReadResource
                }
            });

            return functions;
        }

        /// <summary>
        /// Executes an MCP function call requested by the AI.
        /// </summary>
        /// <param name="function">Function call payload.</param>
        /// <returns>A textual result to send back to the AI.</returns>
        public static async Task<string> ExecuteFunctionAsync(FunctionResult function)
        {
            await Task.Yield();

            try
            {
                JObject arguments = JObject.Parse(function.Function.Arguments);

                string serverName = arguments["serverName"]?.Value<string>();

                if (string.IsNullOrWhiteSpace(serverName))
                {
                    return "Parameter 'serverName' is required.";
                }

                McpServerItem server = McpAgentRepository.GetMcpServer(serverName);

                if (server == null)
                {
                    return $"MCP server '{serverName}' was not found or not exist with this name.";
                }

                if (!server.Enabled)
                {
                    return $"MCP server '{serverName}' was disabled by the user.";
                }

                if (function.Function.Name == FUNCTION_CALL_TOOL)
                {
                    string toolName = arguments["toolName"]?.Value<string>();

                    if (string.IsNullOrWhiteSpace(toolName))
                    {
                        return "Parameter 'toolName' is required.";
                    }

                    string argumentsJson = arguments["argumentsJson"]?.Value<string>();

                    await InvokeOnExecutingFunctionAsync($"Calling MCP tool '{toolName}' on server '{serverName}'...");
                    return await CallTool(server, toolName, argumentsJson);
                }

                if (function.Function.Name == FUNCTION_READ_RESOURCE)
                {
                    string resourceUri = arguments["resourceUri"]?.Value<string>();

                    if (string.IsNullOrWhiteSpace(resourceUri))
                    {
                        return "Parameter 'resourceUri' is required.";
                    }

                    await InvokeOnExecutingFunctionAsync($"Reading MCP resource '{resourceUri}' from server '{serverName}'...");
                    return await ReadResource(server, resourceUri);
                }

                return $"The function {function.Function.Name} not exists.";
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return ex.Message;
            }
        }

        /// <summary>
        /// Builds a context snapshot with MCP server capabilities (tools and resources) to be sent to AI when MCP is added to chat.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <returns>A text payload containing MCP server metadata and capabilities listing.</returns>
        public static async Task<string> BuildServerCapabilitiesContextAsync(McpServerItem server)
        {
            if (server == null)
            {
                return "MCP server was not provided.";
            }

            await InvokeOnExecutingFunctionAsync($"Listing capabilities from MCP server '{server.Name}'...");

            string tools = await ListTools(server);
            string resources = await ListResources(server);

            return string.Concat(
                "MCP Server Name: ", server.Name, Environment.NewLine,
                "MCP Transport Type: ", server.TransportType.ToString(), Environment.NewLine,
                "MCP Tools List (JSON):", Environment.NewLine, tools, Environment.NewLine,
                "MCP Resources List (JSON):", Environment.NewLine, resources);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Invokes the execution event to update UI progress.
        /// </summary>
        /// <param name="message">The progress message.</param>
        private static async Task InvokeOnExecutingFunctionAsync(string message)
        {
            OnExecutingFunction?.Invoke("MCP Agent: " + message);

            await Task.Delay(50);
        }

        /// <summary>
        /// Lists tools available in the configured MCP server.
        /// </summary>
        private static async Task<string> ListTools(McpServerItem server)
        {
            if (server.TransportType == McpTransportType.Sse)
            {
                return await McpSseRuntime.ListToolsAsync(server);
            }

            return await McpStdioRuntime.ListToolsAsync(server);
        }

        /// <summary>
        /// Calls a tool in the configured MCP server.
        /// </summary>
        private static async Task<string> CallTool(McpServerItem server, string toolName, string argumentsJson)
        {
            if (server.TransportType == McpTransportType.Sse)
            {
                return await McpSseRuntime.CallToolAsync(server, toolName, argumentsJson);
            }

            return await McpStdioRuntime.CallToolAsync(server, toolName, argumentsJson);
        }

        /// <summary>
        /// Lists resources available in the configured MCP server.
        /// </summary>
        private static async Task<string> ListResources(McpServerItem server)
        {
            if (server.TransportType == McpTransportType.Sse)
            {
                return await McpSseRuntime.ListResourcesAsync(server);
            }

            return await McpStdioRuntime.ListResourcesAsync(server);
        }

        /// <summary>
        /// Reads a resource in the configured MCP server.
        /// </summary>
        private static async Task<string> ReadResource(McpServerItem server, string resourceUri)
        {
            if (server.TransportType == McpTransportType.Sse)
            {
                return await McpSseRuntime.ReadResourceAsync(server, resourceUri);
            }

            return await McpStdioRuntime.ReadResourceAsync(server, resourceUri);
        }

        #endregion Private Methods
    }
}
