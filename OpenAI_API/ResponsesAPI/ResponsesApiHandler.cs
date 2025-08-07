using Newtonsoft.Json;
using OpenAI_API.ResponsesAPI.Models.Request;
using OpenAI_API.ResponsesAPI.Models.Response;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI_API.ResponsesAPI
{
    /// <summary>
    /// Provides static methods for handling OpenAI API responses within the application.
    /// </summary>
    public static class ResponsesApiHandler
    {
        /// <summary>
        /// Sends an asynchronous HTTP POST request with a <see cref="ComputerUseRequest"/> payload to the configured API endpoint,
        /// including appropriate authorization headers for either Azure or standard OpenAI usage.
        /// </summary>
        /// <param name="request">The <see cref="ComputerUseRequest"/> object to be serialized and sent in the request body.</param>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance used to send the request. Its BaseAddress must be set.</param>
        /// <param name="apiKey">The API key used for authorization. Added as "api-key" header for Azure or "Authorization" Bearer token otherwise.</param>
        /// <param name="isAzure">Indicates whether the request is for an Azure OpenAI endpoint (true) or standard OpenAI (false).</param>
        /// <param name="openAIOrganization">Optional organization ID to include in the "OpenAI-Organization" header if provided.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A <see cref="Task{ComputerUseResponse}"/> representing the asynchronous operation, containing the deserialized response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the HTTP response status code indicates failure.</exception>
        /// <exception cref="JsonSerializationException">Thrown when the response content cannot be deserialized into a <see cref="ComputerUseResponse"/>.</exception>
        public static async Task<ComputerUseResponse> SendComputerUseRequestAsync(ComputerUseRequest request,
                                                                                  HttpClient httpClient,
                                                                                  string apiKey,
                                                                                  bool isAzure = false,
                                                                                  string openAIOrganization = null,
                                                                                  CancellationToken cancellationToken = default)
        {
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Remove("api-key");
            httpClient.DefaultRequestHeaders.Remove("OpenAI-Organization");

            if (isAzure)
            {
                httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
            }
            else
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }

            if (!string.IsNullOrWhiteSpace(openAIOrganization))
            {
                httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", openAIOrganization);
            }

            string json = JsonConvert.SerializeObject(request);
            using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpClient.PostAsync(httpClient.BaseAddress, content, cancellationToken).ConfigureAwait(false);

            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {responseString}");
            }

            ComputerUseResponse result = JsonConvert.DeserializeObject<ComputerUseResponse>(responseString);

            if (result == null)
            {
                throw new JsonSerializationException("Failed to deserialize response to ComputerUseResponse.");
            }

            return result;
        }
    }
}