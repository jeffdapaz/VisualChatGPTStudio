using Newtonsoft.Json.Linq;
using OpenAI_API.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using JeffPires.VisualChatGPTStudio.Agents;
using JeffPires.VisualChatGPTStudio.Utils;
using VisualChatGPTStudioShared.Utils.Http;
using VisualChatGPTStudioShared.Utils.Repositories;
using Parameter = OpenAI_API.Functions.Parameter;

namespace VisualChatGPTStudioShared.Agents.ApiAgent
{
    /// <summary>
    /// Allows interaction with any API as an Agent.
    /// </summary>
    public static class ApiAgent
    {
        public static readonly IReadOnlyList<Tool> Tools =
        [
            new(ExecuteFunctionAsync)
            {
                Name = "call_rest_api",
                Description = "Calls a REST API endpoint using the specified HTTP method, with optional headers, query string, and body, and returns the response.",
                ExampleToSystemMessage = """
                                         Use this when you need to call RESTful web services, HTTP APIs, or fetch data from web endpoints.
                                         You can also add 'body' for POST/PUT/PATCH methods or 'queryParams' for GET method, in JSON format.

                                         Example usage:
                                         <|tool_call_begin|> functions.call_rest_api:1 <|tool_call_argument_begin|> {"apiName": "Users API", "endPoint": "/{controller}/{action}", "method": "GET", "headers": { "Authorization": "Bearer token" }} <|tool_call_end|>
                                         """,
                RiskLevel = RiskLevel.Medium,
                Category = "API",
                Approval = ApprovalKind.AutoApprove,
                Properties = new Dictionary<string, Property>
                {
                    { "apiName", new Property { Types = ["string"], Description = "The API's name." } },
                    { "endPoint", new Property { Types = ["string"], Description = "The API endpoint, e.g. /{controller}/{action}." } },
                    { "method", new Property { Types = ["string"], Description = "HTTP method to be used (GET, POST, PUT, DELETE, etc.)." } },
                    { "headers", new Property { Types = ["object", "null"], Description = "Optional headers in JSON format, e.g., { \"param1\": \"value1\" }." } },
                    { "queryParams", new Property { Types = ["object", "null"], Description = "Optional query parameters in JSON format, e.g., { \"param1\": \"value1\" }." } },
                    { "body", new Property { Types = ["string", "null"], Description = "Request body for POST/PUT/PATCH methods, in JSON format." } }
                }
            },
            new(ExecuteFunctionAsync)
            {
                Name = "call_soap_api",
                Description = "Calls a SOAP service using the specified SOAP action and envelope, with optional headers, and returns the response.",
                ExampleToSystemMessage = """
                                         Use this when you need to call RESTful web services, HTTP APIs, or fetch data from web endpoints.
                                         You can also add 'body' for POST/PUT/PATCH methods or 'queryParams' for GET method, in JSON format.

                                         Example usage:
                                         <|tool_call_begin|> functions.call_soap_api:1 <|tool_call_argument_begin|> {"apiName": "Users API", "endPoint": "The SOAP service endpoint, without the base url.", "method": "GET", "headers": { "Authorization": "Bearer token" }} <|tool_call_end|>
                                         """,
                RiskLevel = RiskLevel.Medium,
                Category = "API",
                Approval = ApprovalKind.AutoApprove,
                Properties = new Dictionary<string, Property>
                {
                    { "apiName", new Property { Types = ["string"], Description = "The API's name." } },
                    { "endPoint", new Property { Types = ["string"], Description = "The SOAP service endpoint, without the base url." } },
                    { "soapAction", new Property { Types = ["string"], Description = "SOAP Action to be executed." } },
                    { "headers", new Property { Types = ["object", "null"], Description = "Optional headers in JSON format, e.g., { \"param1\": \"value1\" }." } },
                    { "soapEnvelope", new Property { Types = ["string"], Description = "SOAP envelope in XML format." } }
                }
            },
        ];

        /// <summary>
        /// Executes an asynchronous function call to an API, handling both SOAP and REST requests based on the provided function parameters.
        /// </summary>
        /// <param name="tool">The function result containing the API call details.</param>
        /// <returns>A tuple where the first value refers to the response to be sent to the AI, and the second value refers to the API response to be displayed in the chat, when applicable.</returns>
        private static async Task<ToolResult> ExecuteFunctionAsync(Tool tool, IReadOnlyDictionary<string, object> args)
        {
            try
            {
                var apiName = args.GetString("apiName");
                var apiDefinition = ApiAgentRepository.GetAPI(apiName);

                if (apiDefinition == null)
                {
                    return new ToolResult
                    {
                        IsSuccess = false,
                        Result = $"API with name {apiName} was not found."
                    };
                }

                var endPoint = args.GetString("endPoint");

                if (!endPoint.StartsWith("/"))
                {
                    endPoint = "/" + endPoint;
                }

                // Optional headers
                var headers = args.GetObject<Dictionary<string, string>>("headers") ?? [];
                foreach (ApiTagItem tag in apiDefinition.Tags.Where(t => t.Type == ApiTagType.Header))
                {
                    headers[tag.Key] = tag.Value;
                }

                HttpStatusCode responseStatusCode;
                string responseContent;

                if (tool.Name == "call_soap_api")
                {
                    var soapAction = args.GetString("soapAction");
                    var soapEnvelope = args.GetString("soapEnvelope");

                    HttpResponseMessage response = await CallSoapApiAsync(apiDefinition, endPoint, soapAction, headers, soapEnvelope, tool.LogResponseAndRequest);

                    responseStatusCode = response.StatusCode;
                    responseContent = FormatXml(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    var method = args.GetString("method");

                    // Optional query parameters
                    var queryParams = args.GetObject<Dictionary<string, string>>("queryParams") ?? [];

                    // Request body (for POST, PUT, PATCH, etc.)
                    var body = args.GetString("body");

                    foreach (ApiTagItem tag in apiDefinition.Tags.Where(t => t.Type == ApiTagType.QueryString))
                    {
                        queryParams[tag.Key] = tag.Value;
                    }

                    var response = await CallRestApiAsync(apiDefinition, endPoint, method, headers, queryParams, body, tool.LogResponseAndRequest);

                    responseStatusCode = response.StatusCode;
                    var jsonString = JsonUtils.PrettyPrintFormat(await response.Content.ReadAsStringAsync());
                    responseContent = string.Join(Environment.NewLine, "```json", jsonString,  "```");
                }

                if (apiDefinition.SendResponsesToAI)
                {
                    return new ToolResult
                    {
                        Result = $"Response Status Code: {responseStatusCode}{Environment.NewLine}{responseContent}"
                    };
                }

                if (responseStatusCode != HttpStatusCode.OK)
                {
                    responseContent = null;
                }

                return new ToolResult
                {
                    Result = $"Response Status Code: {responseStatusCode}. The data is displayed to the user. The user in the plugin settings did not allow data to be transferred to the agent from the API.",
                    PrivateResult = responseContent
                };
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return new ToolResult
                {
                    IsSuccess = false,
                    Result = ex.Message
                };
            }
        }

        #region Public Methods

        /// <summary>
        /// Static constructor for the ApiAgent class. Initializes the class by creating the database through the ApiAgentRepository.
        /// </summary>
        static ApiAgent()
        {
            ApiAgentRepository.CreateDataBase();
        }

        /// <summary>
        /// Retrieves a list of API definitions, ordered by their names.
        /// </summary>
        /// <returns>
        /// A sorted list of API items.
        /// </returns>
        public static List<ApiItem> GetAPIsDefinitions()
        {
            return ApiAgentRepository.GetAPIs().OrderBy(x => x.Name).ToList();
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Performs the SOAP API call synchronously.
        /// </summary>
        /// <param name="apiDefinition">The API's definition.</param>
        /// <param name="endPoint">The endpoint to be called.</param>
        /// <param name="soapAction">SOAP Action to be used.</param>
        /// <param name="headers">Optional headers.</param>
        /// <param name="soapEnvelope">SOAP envelope in XML format.</param>
        /// <param name="logRequestAndResponse">Indicate whether the request and response should be logged..</param>
        /// <returns>The API response.</returns>
        private static async Task<HttpResponseMessage> CallSoapApiAsync(ApiItem apiDefinition,
                                                                        string endPoint,
                                                                        string soapAction,
                                                                        Dictionary<string, string> headers,
                                                                        string soapEnvelope,
                                                                        bool logRequestAndResponse)
        {
            using (HttpClient client = new())
            {
                HttpRequestMessage request = new(HttpMethod.Post, apiDefinition.BaseUrl.TrimEnd('/') + endPoint)
                {
                    Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
                };

                // Add SOAPAction header if provided
                if (!string.IsNullOrWhiteSpace(soapAction))
                {
                    request.Headers.Add("SOAPAction", soapAction);
                }

                // Add optional headers if provided
                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                if (logRequestAndResponse)
                {
                    await HttpLogs.LogRequestAsync(request);
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (logRequestAndResponse)
                {
                    await HttpLogs.LogResponseAsync(response);
                }

                return response;
            }
        }

        /// <summary>
        /// Performs the REST API call synchronously.
        /// </summary>
        /// <param name="apiDefinition">The API's definition.</param>
        /// <param name="endPoint">The endpoint to be called.</param>
        /// <param name="method">HTTP method (GET, POST, PUT, DELETE, etc.).</param>
        /// <param name="headers">Optional headers.</param>
        /// <param name="queryParams">Optional query parameters.</param>
        /// <param name="body">Request body (when applicable).</param>
        /// <param name="logRequestAndResponse">Indicate whether the request and response should be logged..</param>
        /// <returns>The API response.</returns>
        private static async Task<HttpResponseMessage> CallRestApiAsync(ApiItem apiDefinition,
                                                                        string endPoint,
                                                                        string method,
                                                                        Dictionary<string, string> headers,
                                                                        Dictionary<string, string> queryParams,
                                                                        string body,
                                                                        bool logRequestAndResponse)
        {
            using (HttpClient client = new())
            {
                // Add query parameters to the URL, if any
                if (queryParams != null && queryParams.Any())
                {
                    string queryString = string.Join("&", queryParams.Select(q => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(q.Value)}"));

                    endPoint += (endPoint.Contains("?") ? "&" : "?") + queryString;
                }

                HttpRequestMessage request = new(new HttpMethod(method), apiDefinition.BaseUrl.TrimEnd('/') + endPoint);

                // Add headers, if provided
                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                // If the method requires a body (e.g., POST, PUT, or PATCH), add it
                if (!string.IsNullOrWhiteSpace(body) && (method.ToUpper() == "POST" || method.ToUpper() == "PUT" || method.ToUpper() == "PATCH"))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                if (logRequestAndResponse)
                {
                    await HttpLogs.LogRequestAsync(request);
                }

                // Send the request and wait for the response synchronously
                HttpResponseMessage response = await client.SendAsync(request);

                if (logRequestAndResponse)
                {
                    await HttpLogs.LogResponseAsync(response);
                }

                return response;
            }
        }

        /// <summary>
        /// Formats the provided XML string with indentation and wraps it in code block syntax for display.
        /// </summary>
        /// <param name="xml">The XML string to be formatted.</param>
        /// <returns>A formatted XML string wrapped in code block syntax.</returns>
        private static string FormatXml(string xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return null;
                }

                StringBuilder stringBuilder = new();

                XDocument xmlDoc = XDocument.Parse(xml);

                using (StringWriter stringWriter = new(stringBuilder))
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true }))
                    {
                        xmlDoc.WriteTo(xmlWriter);
                    }
                }

                return string.Concat("```xml", Environment.NewLine, stringBuilder.ToString(), Environment.NewLine, "```");
            }
            catch (Exception)
            {
                return xml;
            }
        }

        #endregion Private Methods
    }
}
