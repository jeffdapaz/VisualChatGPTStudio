using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace VisualChatGPTStudioShared.Utils.Http
{
    /// <summary>
    /// Represents a class for handling HTTP logs, which may include logging HTTP requests, responses, and related metadata.
    /// </summary>
    public class HttpLogs
    {
        /// <summary>
        /// Logs the details of an HTTP request, including URI, method, headers, and content if available.
        /// </summary>
        public static async Task LogRequestAsync(HttpRequestMessage request)
        {
            Logger.Log($"Request URI: {request.RequestUri}");
            Logger.Log($"Request Method: {request.Method}");
            Logger.Log("Request Headers:");

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                Logger.Log($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            if (request.Content != null)
            {
                string content = await request.Content.ReadAsStringAsync();

                Logger.Log("Request Content: " + content);
            }

            Logger.Log(new string('_', 100));
        }

        /// <summary>
        /// Logs the details of an HTTP response, including headers, status code, and content (if available).
        /// </summary>
        public static async Task LogResponseAsync(HttpResponseMessage response)
        {
            if (response == null)
            {
                return;
            }

            Logger.Log("Response Headers:");

            foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
            {
                Logger.Log($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            Logger.Log($"Response Status Code: {response.StatusCode}");

            if (response.Content != null)
            {
                string content = await response.Content.ReadAsStringAsync();

                Logger.Log("Response Content: " + content);
            }

            Logger.Log(new string('_', 100));
        }
    }
}