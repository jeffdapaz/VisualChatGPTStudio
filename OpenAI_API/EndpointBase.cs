﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI_API
{
    /// <summary>
    /// A base object for any OpenAI API endpoint, encompassing common functionality
    /// </summary>
    public abstract class EndpointBase
    {
        private const string UserAgent = "Visual_chatGPT_Studio";

        /// <summary>
        /// The internal reference to the API, mostly used for authentication
        /// </summary>
        protected readonly OpenAIAPI _Api;

        /// <summary>
        /// Constructor of the api endpoint base, to be called from the contructor of any devived classes.  Rather than instantiating any endpoint yourself, access it through an instance of <see cref="OpenAIAPI"/>.
        /// </summary>
        /// <param name="api"></param>
        internal EndpointBase(OpenAIAPI api)
        {
            this._Api = api;
        }

        /// <summary>
        /// The name of the endpoint, which is the final path segment in the API URL.  Must be overriden in a derived class.
        /// </summary>
        protected abstract string Endpoint { get; }

        /// <summary>
        /// Gets the URL of the endpoint, based on the base OpenAI API URL followed by the endpoint name.  For example "https://api.openai.com/v1/completions"
        /// </summary>
        protected string Url
        {
            get
            {
                return string.Format(_Api.ApiUrlFormat, _Api.ApiVersion, Endpoint);
            }
        }

        /// <summary>
        /// Gets an HTTPClient with the appropriate authorization and other headers set
        /// </summary>
        /// <returns>The fully initialized HttpClient</returns>
        /// <exception cref="AuthenticationException">Thrown if there is no valid authentication.</exception>
        protected HttpClient GetClient()
        {
            if (_Api.Auth?.ApiKey is null)
            {
                throw new AuthenticationException("You must provide API authentication.");
            }

            HttpClient client;
            IHttpClientFactory clientFactory = _Api.HttpClientFactory;
            if (clientFactory != null)
            {
                client = clientFactory.CreateClient();
            }
            else
            {
                client = new HttpClient();
            }

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _Api.Auth.ApiKey);
            // Further authentication-header used for Azure openAI service
            client.DefaultRequestHeaders.Add("api-key", _Api.Auth.ApiKey);
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            if (!string.IsNullOrEmpty(_Api.Auth.OpenAIOrganization))
            {
                client.DefaultRequestHeaders.Add("OpenAI-Organization", _Api.Auth.OpenAIOrganization);
            }

            return client;
        }

        /// <summary>
        /// Formats a human-readable error message relating to calling the API and parsing the response
        /// </summary>
        /// <param name="resultAsString">The full content returned in the http response</param>
        /// <param name="response">The http response object itself</param>
        /// <param name="name">The name of the endpoint being used</param>
        /// <param name="description">Additional details about the endpoint of this request (optional)</param>
        /// <returns>A human-readable string error message.</returns>
        protected string GetErrorMessage(string resultAsString, HttpResponseMessage response, string name, string description = "")
        {
            return $"Error at {name} ({description}) with HTTP status code: {response.StatusCode}. Content: {resultAsString ?? "<no content>"}";
        }


        /// <summary>
        /// Sends an HTTP request and returns the response.  Does not do any parsing, but does do error handling.
        /// </summary>
        /// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
        /// <param name="verb">(optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get"/>".  If omitted, then "GET" is assumed.</param>
        /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
        /// <param name="streaming">(optional) If true, streams the response.  Otherwise waits for the entire response before returning.</param>
        /// <returns>The HttpResponseMessage of the response, which is confirmed to be successful.</returns>
        /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
        private async Task<HttpResponseMessage> HttpRequestRaw(string url = null, HttpMethod verb = null, object postData = null, bool streaming = false)
        {
            if (string.IsNullOrEmpty(url))
            {
                url = this.Url;
            }

            if (verb == null)
            {
                verb = HttpMethod.Get;
            }

            using HttpClient client = GetClient();

            HttpResponseMessage response = null;
            string resultAsString = null;
            HttpRequestMessage req = new HttpRequestMessage(verb, url);

            if (postData != null)
            {
                if (postData is HttpContent)
                {
                    req.Content = postData as HttpContent;
                }
                else
                {
                    string jsonContent = JsonConvert.SerializeObject(postData, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                    StringContent stringContent = new StringContent(jsonContent, UnicodeEncoding.UTF8, "application/json");
                    req.Content = stringContent;
                }
            }

            response = await client.SendAsync(req, streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            else
            {
                try
                {
                    resultAsString = await response.Content.ReadAsStringAsync();
                }
                catch (Exception readError)
                {
                    resultAsString = "Additionally, the following error was thrown when attempting to read the response content: " + readError.ToString();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException("API rejected your authorization. Full API response follows: " + resultAsString);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw new HttpRequestException("API had an internal server error. Please retry your request.  " + GetErrorMessage(resultAsString, response, Endpoint, url));
                }
                else if ((int)response.StatusCode == 429)
                {
                    // Handle rate limit: retry after specified time if Retry-After header exists
                    if (response.Headers.TryGetValues("Retry-After", out IEnumerable<string> values))
                    {
                        string retryAfterValue = values.FirstOrDefault();

                        if (int.TryParse(retryAfterValue, out int delaySeconds))
                        {
                            await Task.Delay(delaySeconds * 1000);

                            // Retry the request recursively
                            return await HttpRequestRaw(url, verb, postData, streaming);
                        }
                        else
                        {
                            // Retry-After header exists but is not a valid integer, rethrow
                            throw new HttpRequestException(GetErrorMessage(resultAsString, response, Endpoint, url));
                        }
                    }
                    else
                    {
                        // No Retry-After header, rethrow
                        throw new HttpRequestException(GetErrorMessage(resultAsString, response, Endpoint, url));
                    }
                }
                else
                {
                    HttpRequestException errorToThrow = new HttpRequestException(GetErrorMessage(resultAsString, response, Endpoint, url));

                    ApiErrorResponse parsedError = JsonConvert.DeserializeObject<ApiErrorResponse>(resultAsString);

                    try
                    {
                        errorToThrow.Data.Add("message", parsedError.Error.Message);
                        errorToThrow.Data.Add("type", parsedError.Error.ErrorType);
                        errorToThrow.Data.Add("param", parsedError.Error.Parameter);
                        errorToThrow.Data.Add("code", parsedError.Error.ErrorCode);
                    }
                    catch (Exception parsingError)
                    {
                        throw new HttpRequestException(errorToThrow.Message, parsingError);
                    }

                    throw errorToThrow;
                }
            }
        }

        /// <summary>
        /// Sends an HTTP Get request and return the string content of the response without parsing, and does error handling.
        /// </summary>
        /// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
        /// <returns>The text string of the response, which is confirmed to be successful.</returns>
        /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
        internal async Task<string> HttpGetContent<T>(string url = null)
        {
            HttpResponseMessage response = await HttpRequestRaw(url);
            return await response.Content.ReadAsStringAsync();
        }


        /// <summary>
        /// Sends an HTTP Request and does initial parsing
        /// </summary>
        /// <typeparam name="T">The <see cref="ApiResultBase"/>-derived class for the result</typeparam>
        /// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
        /// <param name="verb">(optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get"/>".  If omitted, then "GET" is assumed.</param>
        /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
        /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T"/></returns>
        /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned or if the result couldn't be parsed.</exception>
        private async Task<T> HttpRequest<T>(string url = null, HttpMethod verb = null, object postData = null) where T : ApiResultBase
        {
            HttpResponseMessage response = await HttpRequestRaw(url, verb, postData);
            string resultAsString = await response.Content.ReadAsStringAsync();

            T res = JsonConvert.DeserializeObject<T>(resultAsString);

            try
            {
                res.RequestId = response.Headers.GetValues("X-Request-ID").FirstOrDefault();

                if (string.IsNullOrEmpty(res.Model))
                {
                    res.Model = response.Headers.GetValues("Openai-Model").FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                Debug.Print($"Issue parsing metadata of OpenAi Response.  Url: {url}, Error: {e.ToString()}, Response: {resultAsString}.  This is probably ignorable.");
            }

            return res;
        }

        /// <summary>
        /// Sends an HTTP Get request and does initial parsing
        /// </summary>
        /// <typeparam name="T">The <see cref="ApiResultBase"/>-derived class for the result</typeparam>
        /// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
        /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T"/></returns>
        /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned or if the result couldn't be parsed.</exception>
        internal async Task<T> HttpGet<T>(string url = null) where T : ApiResultBase
        {
            return await HttpRequest<T>(url, HttpMethod.Get);
        }

        /// <summary>
        /// Sends an HTTP Post request and does initial parsing
        /// </summary>
        /// <typeparam name="T">The <see cref="ApiResultBase"/>-derived class for the result</typeparam>
        /// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
        /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
        /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T"/></returns>
        /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned or if the result couldn't be parsed.</exception>
        internal async Task<T> HttpPost<T>(string url = null, object postData = null) where T : ApiResultBase
        {
            return await HttpRequest<T>(url, HttpMethod.Post, postData);
        }

        /// <summary>
        /// Sends an HTTP Delete request and does initial parsing
        /// </summary>
        /// <typeparam name="T">The <see cref="ApiResultBase"/>-derived class for the result</typeparam>
        /// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
        /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
        /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T"/></returns>
        /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned or if the result couldn't be parsed.</exception>
        internal async Task<T> HttpDelete<T>(string url = null, object postData = null) where T : ApiResultBase
        {
            return await HttpRequest<T>(url, HttpMethod.Delete, postData);
        }


        /// <summary>
        /// Sends an HTTP Put request and does initial parsing
        /// </summary>
        /// <typeparam name="T">The <see cref="ApiResultBase"/>-derived class for the result</typeparam>
        /// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
        /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
        /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T"/></returns>
        /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned or if the result couldn't be parsed.</exception>
        internal async Task<T> HttpPut<T>(string url = null, object postData = null) where T : ApiResultBase
        {
            return await HttpRequest<T>(url, HttpMethod.Put, postData);
        }



        /*
		/// <summary>
		/// Sends an HTTP request and handles a streaming response.  Does basic line splitting and error handling.
		/// </summary>
		/// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
		/// <param name="verb">(optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get"/>".  If omitted, then "GET" is assumed.</param>
		/// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
		/// <returns>The HttpResponseMessage of the response, which is confirmed to be successful.</returns>
		/// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
		private async IAsyncEnumerable<string> HttpStreamingRequestRaw(string url = null, HttpMethod verb = null, object postData = null)
		{
			var response = await HttpRequestRaw(url, verb, postData, true);

			using (var stream = await response.Content.ReadAsStreamAsync())
			using (StreamReader reader = new StreamReader(stream))
			{
				string line;
				while ((line = await reader.ReadLineAsync()) != null)
				{
					if (line.StartsWith("data: "))
						line = line.Substring("data: ".Length);
					if (line == "[DONE]")
					{
						yield break;
					}
					else if (!string.IsNullOrWhiteSpace(line))
					{
						yield return line.Trim();
					}
				}
			}
		}
		*/


        /// <summary>
        /// Sends an HTTP request and handles a streaming response.  Does basic line splitting and error handling.
        /// </summary>
        /// <param name="url">(optional) If provided, overrides the url endpoint for this request.  If omitted, then <see cref="Url"/> will be used.</param>
        /// <param name="verb">(optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get"/>".  If omitted, then "GET" is assumed.</param>
        /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
        /// <returns>The HttpResponseMessage of the response, which is confirmed to be successful.</returns>
        /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
        protected async IAsyncEnumerable<T> HttpStreamingRequest<T>(string url = null, HttpMethod verb = null, object postData = null) where T : ApiResultBase
        {
            HttpResponseMessage response = await HttpRequestRaw(url, verb, postData, true);

            string organization = null;
            string requestId = null;
            TimeSpan processingTime = TimeSpan.Zero;
            string openaiVersion = null;
            string modelFromHeaders = null;

            try
            {
                organization = response.Headers.GetValues("Openai-Organization").FirstOrDefault();
                requestId = response.Headers.GetValues("X-Request-ID").FirstOrDefault();
                processingTime = TimeSpan.FromMilliseconds(int.Parse(response.Headers.GetValues("Openai-Processing-Ms").First()));
                openaiVersion = response.Headers.GetValues("Openai-Version").FirstOrDefault();
                modelFromHeaders = response.Headers.GetValues("Openai-Model").FirstOrDefault();
            }
            catch (Exception e)
            {
                Debug.Print($"Issue parsing metadata of OpenAi Response.  Url: {url}, Error: {e.ToString()}.  This is probably ignorable.");
            }

            string resultAsString = "";

            using (Stream stream = await response.Content.ReadAsStreamAsync())
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    resultAsString += line + Environment.NewLine;

                    if (line.StartsWith("data:"))
                    {
                        line = line.Substring("data:".Length);
                    }

                    line = line.TrimStart();

                    if (line == "[DONE]")
                    {
                        yield break;
                    }
                    else if (line.StartsWith(":"))
                    { }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        T res = JsonConvert.DeserializeObject<T>(line);

                        res.Organization = organization;
                        res.RequestId = requestId;
                        res.ProcessingTime = processingTime;
                        res.OpenaiVersion = openaiVersion;
                        if (string.IsNullOrEmpty(res.Model))
                        {
                            res.Model = modelFromHeaders;
                        }

                        yield return res;
                    }
                }
            }
        }

        internal class ApiErrorResponse
        {
            /// <summary>
            /// The error details
            /// </summary>
            [JsonProperty("error")]
            public ApiErrorResponseError Error { get; set; }
        }
        internal class ApiErrorResponseError
        {
            /// <summary>
            /// The error message
            /// </summary>
            [JsonProperty("message")]

            public string Message { get; set; }

            /// <summary>
            /// The type of error
            /// </summary>
            [JsonProperty("type")]
            public string ErrorType { get; set; }

            /// <summary>
            /// The parameter that caused the error
            /// </summary>
            [JsonProperty("param")]

            public string Parameter { get; set; }

            /// <summary>
            /// The error code
            /// </summary>
            [JsonProperty("code")]
            public string ErrorCode { get; set; }
        }
    }
}
