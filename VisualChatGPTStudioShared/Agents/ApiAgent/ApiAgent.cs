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

        /// <summary>
        /// Returns a list of functions that the AI can call to interact with an API.
        /// </summary>
        /// <returns>List of <see cref="FunctionRequest"/>.</returns>
        public static List<FunctionRequest> GetApiFunctions()
        {
            List<FunctionRequest> functions = [];

            Parameter parametersForRestApi = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    { "apiName", new Property { Types = ["string"], Description = "The API's name." } },
                    { "endPoint", new Property { Types = ["string"], Description = "The API endpoint, e.g. /{controler/{action}." } },
                    { "method", new Property { Types = ["string"], Description = "HTTP method to be used (GET, POST, PUT, DELETE, etc.)." } },
                    { "headers", new Property { Types = ["object", "null"], Description = "Optional headers in JSON format, e.g., { \"param1\": \"value1\" }." } },
                    { "queryParams", new Property { Types = ["object", "null"], Description = "Optional query parameters in JSON format, e.g., { \"param1\": \"value1\" }." } },
                    { "body", new Property { Types = ["string", "null"], Description = "Request body for POST/PUT/PATCH methods, in JSON format." } }
                }
            };

            FunctionRequest functionRequestRestApi = new()
            {
                Function = new Function
                {
                    Name = nameof(CallRestApiAsync),
                    Description = "Calls a REST API endpoint using the specified HTTP method, with optional headers, query string, and body, and returns the response.",
                    Parameters = parametersForRestApi
                }
            };

            Parameter parametersForSoapApi = new()
            {
                Properties = new Dictionary<string, Property>
                {
                    { "apiName", new Property { Types = ["string"], Description = "The API's name." } },
                    { "endPoint", new Property { Types = ["string"], Description = "The SOAP service endpoint, without the base url." } },
                    { "soapAction", new Property { Types = ["string"], Description = "SOAP Action to be executed." } },
                    { "headers", new Property { Types = ["object", "null"], Description = "Optional headers in JSON format, e.g., { \"param1\": \"value1\" }." } },
                    { "soapEnvelope", new Property { Types = ["string"], Description = "SOAP envelope in XML format." } }
                }
            };

            FunctionRequest functionRequestSoapApi = new()
            {
                Function = new Function
                {
                    Name = nameof(CallSoapApiAsync),
                    Description = "Calls a SOAP service using the specified SOAP action and envelope, with optional headers, and returns the response.",
                    Parameters = parametersForSoapApi
                }
            };


            functions.Add(functionRequestRestApi);
            functions.Add(functionRequestSoapApi);

            return functions;
        }

        /// <summary>
        /// Executes an asynchronous function call to an API, handling both SOAP and REST requests based on the provided function parameters.
        /// </summary>
        /// <param name="function">The function result containing the API call details.</param>
        /// <param name="logRequestAndResponse">A boolean indicating whether to log the request and response.</param>
        /// <returns>A tuple where the first value refers to the response to be sent to the AI, and the second value refers to the API response to be displayed in the chat, when applicable.</returns>
        public static async Task<(string, string)> ExecuteFunctionAsync(FunctionResult function, bool logRequestAndResponse)
        {
            try
            {
                JObject arguments = JObject.Parse(function.Function.Arguments);

                string apiName = arguments[nameof(apiName)]?.Value<string>();

                ApiItem apiDefinition = ApiAgentRepository.GetAPI(apiName);

                if (apiDefinition == null)
                {
                    return ($"API with name {apiName} was not found.", null);
                }

                string endPoint = arguments[nameof(endPoint)]?.Value<string>();

                if (!endPoint.StartsWith("/"))
                {
                    endPoint = "/" + endPoint;
                }

                // Optional headers
                Dictionary<string, string> headers = arguments[nameof(headers)]?.ToObject<Dictionary<string, string>>() ?? [];

                foreach (ApiTagItem tag in apiDefinition.Tags.Where(t => t.Type == ApiTagType.Header))
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

                HttpStatusCode responseStatusCode;
                string responseContent;

                if (function.Function.Name == nameof(CallSoapApiAsync))
                {
                    string soapAction = arguments["soapAction"]?.Value<string>();
                    string soapEnvelope = arguments["soapEnvelope"]?.Value<string>();

                    HttpResponseMessage response = await CallSoapApiAsync(apiDefinition, endPoint, soapAction, headers, soapEnvelope, logRequestAndResponse);

                    responseStatusCode = response.StatusCode;
                    responseContent = FormatXml(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    string method = arguments[nameof(method)]?.Value<string>();

                    // Optional query parameters
                    Dictionary<string, string> queryParams = arguments[nameof(queryParams)]?.ToObject<Dictionary<string, string>>() ?? [];

                    // Request body (for POST, PUT, PATCH, etc.)
                    string body = arguments[nameof(body)]?.Value<string>();

                    foreach (ApiTagItem tag in apiDefinition.Tags.Where(t => t.Type == ApiTagType.QueryString))
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

                    HttpResponseMessage response = await CallRestApiAsync(apiDefinition, endPoint, method, headers, queryParams, body, logRequestAndResponse);

                    responseStatusCode = response.StatusCode;
                    responseContent = FormatJson(await response.Content.ReadAsStringAsync());
                }

                if (apiDefinition.SendResponsesToAI)
                {
                    return ($"Response Status Code: {responseStatusCode}{Environment.NewLine}{responseContent}", null);
                }

                if (responseStatusCode != HttpStatusCode.OK)
                {
                    responseContent = null;
                }

                return ($"Response Status Code: {responseStatusCode}", responseContent);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);

                return (ex.Message, null);
            }
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

        /// <summary>
        /// Formats a JSON string by parsing it and returning a prettified version enclosed in markdown code block syntax for JSON.
        /// </summary>
        /// <param name="json">The JSON string to be formatted.</param>
        /// <returns>
        /// A string containing the formatted JSON wrapped in markdown code block syntax.
        /// </returns>
        private static string FormatJson(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                JToken parsedJson = JToken.Parse(json);

                return string.Concat("```json", Environment.NewLine, parsedJson.ToString(Newtonsoft.Json.Formatting.Indented), Environment.NewLine, "```");
            }
            catch (Exception)
            {
                return json;
            }
        }

        #endregion Private Methods
    }
}
