using Newtonsoft.Json.Linq;
using OpenAI_API.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VisualChatGPTStudioShared.Utils.Http;
using VisualChatGPTStudioShared.Utils.Repositories;

namespace VisualChatGPTStudioShared.Agents.ApiAgent
{
    /// <summary>
    /// Allows interaction with any REST API as an Agent.
    /// </summary>
    public static class ApiAgent
    {
        /// <summary>
        /// Retrieves a list of API definitions, ordered by their names.
        /// </summary>
        /// <returns>
        /// A sorted list of API items.
        /// </returns>
        public static List<ApiItem> GetAPIsDefinitions()
        {
            ApiAgentRepository.CreateDataBase();

            return ApiAgentRepository.GetAPIs().OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Returns a list of functions that the AI can call to interact with a REST API.
        /// </summary>
        /// <returns>List of <see cref="FunctionRequest"/>.</returns>
        public static List<FunctionRequest> GetRestApiFunctions()
        {
            List<FunctionRequest> functions = [];

            Parameter parameter = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    { "apiName", new Property { Types = ["string"], Description = "The API's name." } },
                    { "endPoint", new Property { Types = ["string"], Description = "The API endpoint, e.g. /{controler/{action}." } },
                    { "method", new Property { Types = ["string"], Description = "HTTP method to be used (GET, POST, PUT, DELETE, etc.)." } },
                    { "headers", new Property { Types = ["object", "null"], Description = "Optional headers in JSON format, e.g., { \"Authorization\": \"Bearer token\" }." } },
                    { "queryParams", new Property { Types = ["object", "null"], Description = "Optional query parameters in JSON format, e.g., { \"param1\": \"value1\" }." } },
                    { "body", new Property { Types = ["string", "null"], Description = "Request body for POST/PUT/PATCH methods, in JSON format." } }
                }
            };

            FunctionRequest functionRequest = new()
            {
                Function = new Function
                {
                    Name = "CallRestApi",
                    Description = "Calls a REST API endpoint using the specified HTTP method, with optional headers, query string, and body, and returns the response.",
                    Parameters = parameter
                }
            };

            functions.Add(functionRequest);

            return functions;
        }

        /// <summary>
        /// Executes the CallRestApi function based on the arguments provided by the AI.
        /// </summary>
        /// <param name="function">The <see cref="FunctionResult"/> object with the function arguments.</param>
        /// <param name="logRequestAndResponse">Indicate whether the request and response should be logged.</param>
        /// <returns>The response returned by the REST API or exception details in case of an error.</returns>
        public static async Task<string> ExecuteFunctionAsync(FunctionResult function, bool logRequestAndResponse)
        {
            try
            {
                JObject arguments = JObject.Parse(function.Function.Arguments);

                string apiName = arguments[nameof(apiName)]?.Value<string>();

                ApiItem apiDefinition = ApiAgentRepository.GetAPI(apiName);

                if (apiDefinition == null)
                {
                    return $"API with name {apiName} was not found.";
                }

                string endPoint = arguments[nameof(endPoint)]?.Value<string>();
                string method = arguments[nameof(method)]?.Value<string>();

                // Optional headers
                Dictionary<string, string> headers = arguments[nameof(headers)]?.ToObject<Dictionary<string, string>>() ?? [];

                // Optional query parameters
                Dictionary<string, string> queryParams = arguments[nameof(queryParams)]?.ToObject<Dictionary<string, string>>() ?? [];

                // Request body (for POST, PUT, PATCH, etc.)
                string body = arguments[nameof(body)]?.Value<string>();

                foreach (ApiTagItem tag in apiDefinition.Tags)
                {
                    if (tag.Type == ApiTagType.Header)
                    {
                        if (headers.ContainsKey(tag.Key))
                        {
                            headers[tag.Key] = tag.Value;
                        }
                        else
                        {
                            headers.Add(tag.Key, tag.Value);
                        }
                    }
                    else if (tag.Type == ApiTagType.QueryString)
                    {
                        if (queryParams.ContainsKey(tag.Key))
                        {
                            queryParams[tag.Key] = tag.Value;
                        }
                        else
                        {
                            queryParams.Add(tag.Key, tag.Value);
                        }
                    }
                }

                return await CallRestApiAsync(apiDefinition, endPoint, method, headers, queryParams, body, logRequestAndResponse);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                return ex.Message;
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
        /// <returns>Response content from the API as a string.</returns>
        private static async Task<string> CallRestApiAsync(ApiItem apiDefinition,
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

                HttpRequestMessage request = new(new HttpMethod(method), apiDefinition.BaseUrl + endPoint);

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

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
