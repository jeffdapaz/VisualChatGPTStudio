using JeffPires.VisualChatGPTStudio.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VisualChatGPTStudioShared.Agents.McpAgent
{
    /// <summary>
    /// Provides MCP stdio runtime operations (initialize, list tools/resources, call tool, read resource).
    /// </summary>
    public static class McpStdioRuntime
    {
        #region Constants

        private const int DefaultTimeoutMs = 30000;
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

            return result?.ToString(Formatting.None) ?? "{}";
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

            return result?.ToString(Formatting.None) ?? "{}";
        }

        /// <summary>
        /// Lists resources available in the target MCP server.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <returns>A JSON string with resources metadata.</returns>
        public static async Task<string> ListResourcesAsync(McpServerItem server)
        {
            JObject result = await ExecuteRequestAsync(server, "resources/list", null);

            return result?.ToString(Formatting.None) ?? "{}";
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

            return result?.ToString(Formatting.None) ?? "{}";
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Executes an MCP request over stdio.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <param name="method">The MCP method name.</param>
        /// <param name="parameters">The optional request parameters.</param>
        /// <returns>The JSON RPC result object.</returns>
        private static async Task<JObject> ExecuteRequestAsync(McpServerItem server, string method, JObject parameters)
        {
            if (server.TransportType != McpTransportType.Stdio)
            {
                throw new InvalidOperationException("Only stdio transport is supported at the moment.");
            }

            Process process = null;

            try
            {
                process = CreateProcess(server);

                if (!process.Start())
                {
                    throw new InvalidOperationException("Unable to start MCP server process.");
                }

                int requestId = 1;

                JObject initializeParams = new()
                {
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"] = new JObject(),
                    ["clientInfo"] = new JObject
                    {
                        ["name"] = Constants.EXTENSION_NAME,
                        ["version"] = "1.0.0"
                    }
                };

                await SendRequestAsync(process, requestId++, "initialize", initializeParams);
                await ReadResponseAsync(process, requestId - 1);

                await SendNotificationAsync(process, "notifications/initialized", new JObject());

                await SendRequestAsync(process, requestId++, method, parameters ?? new JObject());
                return await ReadResponseAsync(process, requestId - 1);
            }
            finally
            {
                if (process != null)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch
                    {
                    }

                    process.Dispose();
                }
            }
        }

        /// <summary>
        /// Creates a process configured for MCP stdio communication.
        /// </summary>
        /// <param name="server">The MCP server configuration.</param>
        /// <returns>The configured process.</returns>
        private static Process CreateProcess(McpServerItem server)
        {
            if (string.IsNullOrWhiteSpace(server.Command))
            {
                throw new InvalidOperationException("MCP server command is empty.");
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = server.Command,
                Arguments = server.Arguments ?? string.Empty,
                WorkingDirectory = string.IsNullOrWhiteSpace(server.WorkingDirectory)
                    ? Environment.CurrentDirectory
                    : server.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            ApplyEnvironmentVariables(startInfo, server.EnvironmentVariablesJson);

            Process process = new()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = false
            };

            return process;
        }

        /// <summary>
        /// Applies environment variables represented by a JSON object.
        /// </summary>
        /// <param name="startInfo">The process start info.</param>
        /// <param name="environmentVariablesJson">Environment variables JSON object.</param>
        private static void ApplyEnvironmentVariables(ProcessStartInfo startInfo, string environmentVariablesJson)
        {
            if (string.IsNullOrWhiteSpace(environmentVariablesJson))
            {
                return;
            }

            JObject env = JObject.Parse(environmentVariablesJson);

            foreach (JProperty property in env.Properties())
            {
                startInfo.EnvironmentVariables[property.Name] = property.Value?.ToString();
            }
        }

        /// <summary>
        /// Sends a JSON RPC request message.
        /// </summary>
        /// <param name="process">The target process.</param>
        /// <param name="id">Request id.</param>
        /// <param name="method">Method name.</param>
        /// <param name="parameters">Parameters object.</param>
        private static async Task SendRequestAsync(Process process, int id, string method, JObject parameters)
        {
            JObject payload = new()
            {
                ["jsonrpc"] = JsonRpcVersion,
                ["id"] = id,
                ["method"] = method,
                ["params"] = parameters ?? new JObject()
            };

            await WriteMessageAsync(process, payload);
        }

        /// <summary>
        /// Sends a JSON RPC notification message.
        /// </summary>
        /// <param name="process">The target process.</param>
        /// <param name="method">Method name.</param>
        /// <param name="parameters">Parameters object.</param>
        private static async Task SendNotificationAsync(Process process, string method, JObject parameters)
        {
            JObject payload = new()
            {
                ["jsonrpc"] = JsonRpcVersion,
                ["method"] = method,
                ["params"] = parameters ?? new JObject()
            };

            await WriteMessageAsync(process, payload);
        }

        /// <summary>
        /// Writes one framed MCP stdio message.
        /// </summary>
        /// <param name="process">The target process.</param>
        /// <param name="payload">The payload object.</param>
        private static async Task WriteMessageAsync(Process process, JObject payload)
        {
            string json = payload.ToString(Formatting.None);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] headerBytes = Encoding.ASCII.GetBytes($"Content-Length: {jsonBytes.Length}\r\n\r\n");

            Stream input = process.StandardInput.BaseStream;

            await input.WriteAsync(headerBytes, 0, headerBytes.Length);
            await input.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            await input.FlushAsync();
        }

        /// <summary>
        /// Reads a JSON RPC response matching the expected id.
        /// </summary>
        /// <param name="process">The target process.</param>
        /// <param name="expectedId">Expected response id.</param>
        /// <returns>The response result object.</returns>
        private static async Task<JObject> ReadResponseAsync(Process process, int expectedId)
        {
            DateTime limit = DateTime.UtcNow.AddMilliseconds(DefaultTimeoutMs);

            while (DateTime.UtcNow <= limit)
            {
                JObject message = await ReadMessageAsync(process);

                if (message == null)
                {
                    continue;
                }

                JToken responseId = message["id"];

                if (responseId == null)
                {
                    continue;
                }

                if (responseId.Type != JTokenType.Integer || responseId.Value<int>() != expectedId)
                {
                    continue;
                }

                if (message["error"] != null)
                {
                    throw new InvalidOperationException(message["error"].ToString(Formatting.None));
                }

                return message["result"] as JObject ?? new JObject();
            }

            throw new TimeoutException("Timeout waiting MCP response.");
        }

        /// <summary>
        /// Reads a single framed MCP stdio message.
        /// </summary>
        /// <param name="process">The target process.</param>
        /// <returns>The parsed message object.</returns>
        private static async Task<JObject> ReadMessageAsync(Process process)
        {
            Stream output = process.StandardOutput.BaseStream;

            int contentLength = await ReadContentLengthAsync(output);

            if (contentLength <= 0)
            {
                return null;
            }

            byte[] payload = await ReadExactAsync(output, contentLength);

            if (payload == null || payload.Length == 0)
            {
                return null;
            }

            string json = Encoding.UTF8.GetString(payload);

            try
            {
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Logger.Log(json);
                return null;
            }
        }

        /// <summary>
        /// Reads the content length from message headers.
        /// </summary>
        /// <param name="stream">The source stream.</param>
        /// <returns>The content length value.</returns>
        private static async Task<int> ReadContentLengthAsync(Stream stream)
        {
            StringBuilder headers = new();
            byte[] one = new byte[1];

            while (true)
            {
                int read = await stream.ReadAsync(one, 0, 1);

                if (read == 0)
                {
                    throw new EndOfStreamException("MCP output stream closed unexpectedly.");
                }

                headers.Append((char)one[0]);

                if (headers.Length >= 4 && headers.ToString(headers.Length - 4, 4) == "\r\n\r\n")
                {
                    break;
                }
            }

            string[] lines = headers.ToString().Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    string lengthPart = line.Substring("Content-Length:".Length).Trim();

                    if (int.TryParse(lengthPart, out int length))
                    {
                        return length;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Reads an exact number of bytes from the stream.
        /// </summary>
        /// <param name="stream">The source stream.</param>
        /// <param name="length">Required byte length.</param>
        /// <returns>The byte array with exact length.</returns>
        private static async Task<byte[]> ReadExactAsync(Stream stream, int length)
        {
            byte[] buffer = new byte[length];
            int totalRead = 0;

            while (totalRead < length)
            {
                int read = await stream.ReadAsync(buffer, totalRead, length - totalRead);

                if (read == 0)
                {
                    throw new EndOfStreamException("MCP output stream closed while reading message body.");
                }

                totalRead += read;
            }

            return buffer;
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
                return new JObject();
            }

            string trimmed = argumentsJson.Trim();

            if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return new JObject();
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
