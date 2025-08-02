using Newtonsoft.Json;
using OpenAI_API.ResponsesAPI.Models.Request;
using OpenAI_API.ResponsesAPI.Models.Response;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI_API.ResponsesAPI
{
    public static class ResponsesApiHandler
    {
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