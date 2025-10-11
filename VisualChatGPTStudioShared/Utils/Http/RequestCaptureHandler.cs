using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VisualChatGPTStudioShared.Utils.Http;

namespace JeffPires.VisualChatGPTStudio.Utils.Http
{
    /// <summary>
    /// Represents a custom HTTP client handler that captures the request data.
    /// </summary>
    /// <param name="options">The app options.</param>
    public class RequestCaptureHandler(OptionPageGridGeneral options) : HttpClientHandler
    {
        /// <summary>
        /// Overrides the SendAsync method to log the request and response information.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (options.AzureEntraIdAuthentication)
            {
                await LoginAzureApiByEntraIdAsync(request);
            }

            request.Headers.Remove("User-Agent");

            request.Headers.Add("User-Agent", Constants.EXTENSION_NAME_UNDERLINED);

            if (options.LogRequests)
            {
                await HttpLogs.LogRequestAsync(request);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (options.LogResponses)
            {
                await HttpLogs.LogResponseAsync(response);
            }

            return response;
        }

        /// <summary>
        /// Logs in to Azure API using Entra ID, handling token acquisition through both cache and interactive login methods.
        /// </summary>
        /// <param name="request">The HTTP request message to which the authorization header will be added.</param>
        private async Task LoginAzureApiByEntraIdAsync(HttpRequestMessage request)
        {
            if (string.IsNullOrWhiteSpace(options.AzureEntraIdApplicationId))
            {
                throw new ArgumentNullException("Application Id", "When choosing to authenticate with Entra ID, you need to define the Application ID.");
            }

            if (string.IsNullOrWhiteSpace(options.AzureEntraIdTenantId))
            {
                throw new ArgumentNullException("Tenant Id", "When choosing to authenticate with Entra ID, you need to define the Tenant ID.");
            }

            string[] scopes = ["https://cognitiveservices.azure.com/.default"];

            IPublicClientApplication app = PublicClientApplicationBuilder.Create(options.AzureEntraIdApplicationId)
            .WithAuthority(AzureCloudInstance.AzurePublic, options.AzureEntraIdTenantId)
            .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
            .Build();

            //Set up token cache persistence in file
            StorageCreationProperties storageProperties = new StorageCreationPropertiesBuilder(
                "msal_cache.dat",                // File name
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.EXTENSION_NAME)) // Path to save
                .Build();

            MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

            cacheHelper.RegisterCache(app.UserTokenCache);

            AuthenticationResult result;

            try
            {
                //Try to obtain the user's token from the cache
                IEnumerable<IAccount> accounts = await app.GetAccountsAsync();

                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
            }
            catch (Exception)
            {
                //If there is no valid token in cache, request interactive login.
                result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
            }

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);
        }
    }
}
