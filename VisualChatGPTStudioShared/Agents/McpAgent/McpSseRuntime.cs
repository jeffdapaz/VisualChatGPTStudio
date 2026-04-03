using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VisualChatGPTStudioShared.Agents.McpAgent
{
    /// <summary>
    /// Provides MCP HTTP/SSE runtime operations (initialize, list tools/resources, call tool, read resource).
    /// </summary>
    public static class McpSseRuntime
    {
        #region Constants

        private const int DefaultTimeoutSeconds = 30;
        private const string JsonRpcVersion = "2.0";

        #endregion Constants

        #region Properties

        #endregion Properties

        #region Constructors

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Lists tools available in the target MCP server.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <returns>A JSON string with tools metadata.</returns>
        public static async Task<string> ListToolsAsync(McpServerItem server)
        {
            JObject result = await ExecuteRequestAsync(server, "tools/list", null);

            return result?.ToString() ?? "{}";
        }

        /// <summary>
        /// Calls a tool in the target MCP server.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <param name="toolName">The tool name.</param>
        /// <param name="argumentsJson">Tool arguments in JSON object format.</param>
        /// <returns>A JSON string with tool execution result.</returns>
        public static async Task<string> CallToolAsync(McpServerItem server, string toolName, string argumentsJson)
        {
            JObject arguments = ParseArguments(argumentsJson);

            JObject parameters = new()
            {
                ["name"] = toolName,
                ["arguments"] = arguments
            };

            JObject result = await ExecuteRequestAsync(server, "tools/call", parameters);

            return result?.ToString() ?? "{}";
        }

        /// <summary>
        /// Lists resources available in the target MCP server.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <returns>A JSON string with resources metadata.</returns>
        public static async Task<string> ListResourcesAsync(McpServerItem server)
        {
            JObject result = await ExecuteRequestAsync(server, "resources/list", null);

            return result?.ToString() ?? "{}";
        }

        /// <summary>
        /// Reads a resource from the target MCP server.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <param name="resourceUri">The resource URI.</param>
        /// <returns>A JSON string with resource contents.</returns>
        public static async Task<string> ReadResourceAsync(McpServerItem server, string resourceUri)
        {
            JObject parameters = new()
            {
                ["uri"] = resourceUri
            };

            JObject result = await ExecuteRequestAsync(server, "resources/read", parameters);

            return result?.ToString() ?? "{}";
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Executes an MCP request over HTTP/SSE endpoint.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <param name="method">The MCP method name.</param>
        /// <param name="parameters">The optional request parameters.</param>
        /// <returns>The JSON RPC result object.</returns>
        private static async Task<JObject> ExecuteRequestAsync(McpServerItem server, string method, JObject parameters)
        {
            if (string.IsNullOrWhiteSpace(server.Endpoint))
            {
                throw new InvalidOperationException("MCP SSE endpoint is empty.");
            }

            if (!Uri.TryCreate(server.Endpoint, UriKind.Absolute, out Uri endpointUri))
            {
                throw new InvalidOperationException("MCP SSE endpoint is invalid.");
            }

            int timeoutSeconds = GetTimeoutSeconds(server.EnvironmentVariablesJson);

            using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };

            Uri postEndpoint = endpointUri;

            if (IsSseEndpoint(server.Endpoint))
            {
                postEndpoint = await DiscoverPostEndpointFromSseAsync(httpClient, server, endpointUri, TimeSpan.FromSeconds(timeoutSeconds));
            }

            int requestId = 1;

            JObject initializeParams = new()
            {
                ["protocolVersion"] = "2024-11-05",
                ["capabilities"] = new JObject(),
                ["clientInfo"] = new JObject
                {
                    ["name"] = "VisualChatGPTStudio",
                    ["version"] = "1.0.0"
                }
            };

            (JObject _, string sessionId) = await SendRequestAsync(httpClient, server, postEndpoint, requestId++, "initialize", initializeParams, expectResponse: true, mcpSessionId: null);
            await SendRequestAsync(httpClient, server, postEndpoint, null, "notifications/initialized", [], expectResponse: false, mcpSessionId: sessionId);

            (JObject result, _) = await SendRequestAsync(httpClient, server, postEndpoint, requestId++, method, parameters ?? [], expectResponse: true, mcpSessionId: sessionId);

            return result;
        }

        /// <summary>
        /// Sends one MCP JSON RPC request to HTTP/SSE endpoint.
        /// </summary>
        /// <param name="httpClient">HTTP client.</param>
        /// <param name="server">MCP server configuration.</param>
        /// <param name="endpoint">Endpoint URI.</param>
        /// <param name="id">Request id, null for notifications.</param>
        /// <param name="method">Method name.</param>
        /// <param name="parameters">Request parameters.</param>
        /// <param name="expectResponse">Indicates whether a response is expected.</param>
        /// <param name="mcpSessionId">Optional MCP session identifier.</param>
        /// <returns>JSON RPC result object and MCP session identifier from headers when available.</returns>
        private static async Task<(JObject, string)> SendRequestAsync(HttpClient httpClient, McpServerItem server, Uri endpoint, int? id, string method, JObject parameters, bool expectResponse, string mcpSessionId)
        {
            JObject payload = new()
            {
                ["jsonrpc"] = JsonRpcVersion,
                ["method"] = method,
                ["params"] = parameters ?? []
            };

            if (id.HasValue)
            {
                payload["id"] = id.Value;
            }

            using HttpRequestMessage request = new(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
            };

            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.Accept.ParseAdd("text/event-stream");

            if (!string.IsNullOrWhiteSpace(mcpSessionId))
            {
                request.Headers.TryAddWithoutValidation("Mcp-Session-Id", mcpSessionId);
            }

            ApplyHttpHeaders(request, server.EnvironmentVariablesJson);

            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = string.Empty;

                try
                {
                    errorBody = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                }

                string endpointText = endpoint?.ToString() ?? "[null]";
                throw new HttpRequestException($"MCP SSE request failed. Method: {method}, Endpoint: {endpointText}, Status: {(int)response.StatusCode} ({response.ReasonPhrase}), Response: {errorBody}");
            }

            if (!expectResponse)
            {
                string sessionIdOnly = ExtractMcpSessionId(response);

                return ([], sessionIdOnly);
            }

            string mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            JObject message;

            if (mediaType.IndexOf("text/event-stream", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                using Stream stream = await response.Content.ReadAsStreamAsync();
                message = await ParseSseMessageFromStreamAsync(stream, id, TimeSpan.FromSeconds(httpClient.Timeout.TotalSeconds));
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                message = ParseResponseMessage(content, id);
            }

            if (message["error"] != null)
            {
                throw new InvalidOperationException(message["error"].ToString());
            }

            string sessionId = ExtractMcpSessionId(response);

            return (message["result"] as JObject ?? [], sessionId);
        }

        /// <summary>
        /// Extracts MCP session identifier from response headers.
        /// </summary>
        /// <param name="response">HTTP response message.</param>
        /// <returns>The MCP session identifier when present; otherwise null.</returns>
        private static string ExtractMcpSessionId(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Mcp-Session-Id", out IEnumerable<string> values))
            {
                return values?.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Detects if configured endpoint is likely an SSE stream endpoint.
        /// </summary>
        /// <param name="endpoint">Endpoint text.</param>
        /// <returns>True if endpoint appears to be SSE stream endpoint; otherwise false.</returns>
        private static bool IsSseEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return false;
            }

            string text = endpoint.ToLowerInvariant();

            return text.Contains("/sse") || text.Contains("transport=sse") || text.Contains("accept=text/event-stream");
        }

        /// <summary>
        /// Connects to SSE stream endpoint and discovers the POST endpoint from an "endpoint" event.
        /// </summary>
        /// <param name="httpClient">HTTP client.</param>
        /// <param name="server">MCP server configuration.</param>
        /// <param name="sseEndpoint">Configured SSE endpoint.</param>
        /// <param name="timeout">Read timeout.</param>
        /// <returns>Discovered POST endpoint URI.</returns>
        private static async Task<Uri> DiscoverPostEndpointFromSseAsync(HttpClient httpClient, McpServerItem server, Uri sseEndpoint, TimeSpan timeout)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, sseEndpoint);
            request.Headers.Accept.ParseAdd("text/event-stream");
            ApplyHttpHeaders(request, server.EnvironmentVariablesJson);

            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = string.Empty;

                try
                {
                    errorBody = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                }

                throw new HttpRequestException($"MCP SSE discovery failed. Endpoint: {sseEndpoint}, Status: {(int)response.StatusCode} ({response.ReasonPhrase}), Response: {errorBody}");
            }

            using Stream stream = await response.Content.ReadAsStreamAsync();
            using StreamReader reader = new(stream, Encoding.UTF8);

            DateTime limit = DateTime.UtcNow.Add(timeout);
            string currentEvent = string.Empty;
            StringBuilder dataBlock = new();

            while (DateTime.UtcNow <= limit)
            {
                Task<string> readLineTask = reader.ReadLineAsync();
                Task completed = await Task.WhenAny(readLineTask, Task.Delay(TimeSpan.FromMilliseconds(250)));

                if (completed != readLineTask)
                {
                    continue;
                }

                string line = readLineTask.Result;

                if (line == null)
                {
                    break;
                }

                if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                {
                    currentEvent = line.Substring(6).Trim();
                    continue;
                }

                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    dataBlock.AppendLine(line.Substring(5).TrimStart());
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line) || dataBlock.Length == 0)
                {
                    continue;
                }

                string payload = dataBlock.ToString().Trim();
                dataBlock.Clear();

                if (!string.Equals(currentEvent, "endpoint", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Uri.TryCreate(payload, UriKind.Absolute, out Uri absoluteUri))
                {
                    return absoluteUri;
                }

                if (Uri.TryCreate(sseEndpoint, payload, out Uri relativeUri))
                {
                    return relativeUri;
                }
            }

            throw new TimeoutException($"Timeout discovering MCP POST endpoint from SSE stream: {sseEndpoint}");
        }

        /// <summary>
        /// Applies custom HTTP headers from JSON configuration.
        /// </summary>
        /// <param name="request">HTTP request message.</param>
        /// <param name="environmentVariablesJson">JSON configuration text.</param>
        private static void ApplyHttpHeaders(HttpRequestMessage request, string environmentVariablesJson)
        {
            JObject headersContainer = TryGetHeadersObject(environmentVariablesJson);

            if (headersContainer == null)
            {
                return;
            }

            foreach (JProperty property in headersContainer.Properties())
            {
                string value = property.Value?.ToString();

                if (!string.IsNullOrWhiteSpace(property.Name) && !string.IsNullOrWhiteSpace(value))
                {
                    request.Headers.TryAddWithoutValidation(property.Name, value);
                }
            }
        }

        /// <summary>
        /// Gets timeout in seconds from JSON configuration.
        /// </summary>
        /// <param name="environmentVariablesJson">JSON configuration text.</param>
        /// <returns>Configured timeout in seconds.</returns>
        private static int GetTimeoutSeconds(string environmentVariablesJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(environmentVariablesJson))
                {
                    return DefaultTimeoutSeconds;
                }

                JObject json = JObject.Parse(environmentVariablesJson);

                int timeout = json["timeoutSeconds"]?.Value<int>() ?? DefaultTimeoutSeconds;

                return timeout > 0 ? timeout : DefaultTimeoutSeconds;
            }
            catch
            {
                return DefaultTimeoutSeconds;
            }
        }

        /// <summary>
        /// Parses one JSON RPC message from an SSE stream.
        /// </summary>
        /// <param name="stream">SSE response stream.</param>
        /// <param name="expectedId">Expected message id.</param>
        /// <param name="timeout">Read timeout.</param>
        /// <returns>The parsed JSON RPC message.</returns>
        private static async Task<JObject> ParseSseMessageFromStreamAsync(Stream stream, int? expectedId, TimeSpan timeout)
        {
            using StreamReader reader = new(stream, Encoding.UTF8);

            DateTime limit = DateTime.UtcNow.Add(timeout);
            StringBuilder dataBlock = new();

            while (DateTime.UtcNow <= limit)
            {
                TimeSpan remaining = limit - DateTime.UtcNow;

                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                Task<string> readLineTask = reader.ReadLineAsync();
                Task completed = await Task.WhenAny(readLineTask, Task.Delay(remaining));

                if (completed != readLineTask)
                {
                    throw new TimeoutException("Timeout waiting SSE message response.");
                }

                string line = await readLineTask;

                if (line == null)
                {
                    break;
                }

                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    dataBlock.AppendLine(line.Substring(5).TrimStart());
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line) || dataBlock.Length == 0)
                {
                    continue;
                }

                JObject message = ParseResponseMessage(dataBlock.ToString(), expectedId);
                dataBlock.Clear();

                if (!expectedId.HasValue || message["id"]?.Value<int>() == expectedId.Value)
                {
                    return message;
                }
            }

            throw new TimeoutException("Timeout waiting SSE message response.");
        }

        /// <summary>
        /// Tries to get headers object from JSON configuration.
        /// </summary>
        /// <param name="environmentVariablesJson">JSON configuration text.</param>
        /// <returns>Headers object or null.</returns>
        private static JObject TryGetHeadersObject(string environmentVariablesJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(environmentVariablesJson))
                {
                    return null;
                }

                JObject json = JObject.Parse(environmentVariablesJson);

                // Preferred shape: { "headers": { "X-ProjectKey": "..." } }
                if (json["headers"] is JObject explicitHeaders)
                {
                    return explicitHeaders;
                }

                // Backward-friendly shape: { "X-ProjectKey": "...", "Another-Header": "..." }
                JObject directHeaders = [];

                foreach (JProperty property in json.Properties())
                {
                    if (property.Name.Equals("timeoutSeconds", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (property.Name.Equals("headers", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (property.Value?.Type == JTokenType.String)
                    {
                        directHeaders[property.Name] = property.Value.Value<string>();
                    }
                }

                return directHeaders.Properties().Any() ? directHeaders : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parses HTTP response content as JSON RPC response. Supports plain JSON and SSE payloads.
        /// </summary>
        /// <param name="content">Raw response content.</param>
        /// <param name="expectedId">Expected request id.</param>
        /// <returns>The parsed JSON RPC response object.</returns>
        private static JObject ParseResponseMessage(string content, int? expectedId)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return [];
            }

            string trimmed = content.Trim();

            // Standard JSON-RPC payload
            if (trimmed.StartsWith("{"))
            {
                return JObject.Parse(trimmed);
            }

            // SSE payload fallback
            List<JObject> messages = ParseSseMessages(trimmed);

            if (messages.Count == 0)
            {
                throw new InvalidOperationException("Unable to parse MCP SSE response content.");
            }

            if (!expectedId.HasValue)
            {
                return messages[0];
            }

            JObject matching = messages.Find(m => m["id"] != null && m["id"].Type == JTokenType.Integer && m["id"].Value<int>() == expectedId.Value);

            return matching ?? messages[0];
        }

        /// <summary>
        /// Parses SSE content and extracts JSON message payloads from "data:" fields.
        /// </summary>
        /// <param name="sseContent">Raw SSE payload.</param>
        /// <returns>List of parsed JSON messages.</returns>
        private static List<JObject> ParseSseMessages(string sseContent)
        {
            List<JObject> messages = [];
            StringBuilder dataBlock = new();

            using StringReader reader = new(sseContent);

            while (true)
            {
                string line = reader.ReadLine();

                if (line == null)
                {
                    if (dataBlock.Length > 0)
                    {
                        TryAddMessage(messages, dataBlock.ToString());
                    }

                    break;
                }

                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    string data = line.Substring(5).TrimStart();
                    dataBlock.AppendLine(data);
                }
                else if (string.IsNullOrWhiteSpace(line) && dataBlock.Length > 0)
                {
                    TryAddMessage(messages, dataBlock.ToString());
                    dataBlock.Clear();
                }
            }

            return messages;
        }

        /// <summary>
        /// Tries to parse and append one JSON message.
        /// </summary>
        /// <param name="messages">Messages output list.</param>
        /// <param name="payload">JSON payload text.</param>
        private static void TryAddMessage(List<JObject> messages, string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            string trimmed = payload.Trim();

            if (!trimmed.StartsWith("{"))
            {
                return;
            }

            try
            {
                messages.Add(JObject.Parse(trimmed));
            }
            catch
            {
            }
        }

        /// <summary>
        /// Parses tool arguments JSON text.
        /// </summary>
        /// <param name="argumentsJson">Arguments JSON text.</param>
        /// <returns>A JObject for MCP tool arguments.</returns>
        private static JObject ParseArguments(string argumentsJson)
        {
            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                return [];
            }

            string trimmed = argumentsJson.Trim();

            if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return [];
            }

            JToken token = JToken.Parse(trimmed);

            if (token is not JObject obj)
            {
                throw new InvalidOperationException("argumentsJson must be a JSON object.");
            }

            return obj;
        }

        #endregion Private Methods
    }
}
