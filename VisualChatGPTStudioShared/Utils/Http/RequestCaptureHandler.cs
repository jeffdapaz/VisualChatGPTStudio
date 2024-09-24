using Azure.Core;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Utils.Http
{
    /// <summary>
    /// Represents a custom HTTP client handler that captures the request data.
    /// </summary>
    public class RequestCaptureHandler : HttpClientHandler
    {
        private readonly bool logRequests;
        private readonly bool logResponses;
        private readonly bool useVisualStudioIdentity;
        private static readonly TokenCredential tokenCredential = new CachedTokenCredential(new VisualStudioCredential());
        private static readonly string[] scopes = ["https://cognitiveservices.azure.com/.default"];

        /// <summary>
        /// Initializes a new instance of the RequestCaptureHandler class.
        /// </summary>
        /// <param name="logRequests">A boolean value indicating whether requests should be logged.</param>
        /// <param name="logResponses">A boolean value indicating whether responses should be logged.</param>
        /// <param name="useVisualStudioIdentity">A boolean value indicating whether authentication should use Managed Identity.</param>
        public RequestCaptureHandler(bool logRequests, bool logResponses, bool useVisualStudioIdentity)
        {
            this.logRequests = logRequests;
            this.logResponses = logResponses;
            this.useVisualStudioIdentity = useVisualStudioIdentity;
        }

        /// <summary>
        /// Overrides the SendAsync method to log the request and response information.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string content;

            if (logRequests)
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
                    content = await request.Content.ReadAsStringAsync();

                    Logger.Log("Request Content: " + content);
                }

                Logger.Log(new string('_', 100));
            }

            if (useVisualStudioIdentity)
            {
                var token = await tokenCredential.GetTokenAsync(new(scopes), cancellationToken);

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (response != null && logResponses)
            {
                Logger.Log("Response Headers:");

                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                {
                    Logger.Log($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                Logger.Log($"Response Status Code: {response.StatusCode}");

                if (response.Content != null)
                {
                    content = await response.Content.ReadAsStringAsync();

                    Logger.Log("Response Content: " + content);
                }

                Logger.Log(new string('_', 100));
            }

            return response;
        }

        public class CachedTokenCredential(TokenCredential wrapped) : TokenCredential
        {
            private const int TokenExpirationOffset = 5;
            private AccessToken? token = null;
            private SemaphoreSlim tokenSemaphore = new(1);
            private readonly TokenCredential wrapped = wrapped;

            public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
            {
                if (!HasValidToken())
                {
                    await RenewTokenAsync(requestContext, cancellationToken);
                }

                return token.Value!;
            }

            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                if (!HasValidToken())
                {
                    RenewToken(requestContext, cancellationToken);
                }

                return token.Value!;
            }

            private async Task RenewTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                await tokenSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (HasValidToken())
                    {
                        return;
                    }

                    token = await wrapped.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    tokenSemaphore.Release();
                }
            }

            private void RenewToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                tokenSemaphore.Wait(cancellationToken);
                try
                {
                    if (HasValidToken())
                    {
                        return;
                    }

                    token = wrapped.GetToken(requestContext, cancellationToken);
                }
                finally
                {
                    tokenSemaphore.Release();
                }
            }

            private bool HasValidToken()
            {
                return token != null
                    && token.Value.ExpiresOn > DateTime.UtcNow.AddSeconds(TokenExpirationOffset);
            }
        }
    }
}