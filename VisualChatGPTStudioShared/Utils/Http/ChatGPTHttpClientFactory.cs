﻿using JeffPires.VisualChatGPTStudio.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;

namespace JeffPires.VisualChatGPTStudio.Utils.Http
{
    /// <summary>
    /// This class provides a factory for creating HttpClient instances for use with the ChatGPT API.
    /// </summary>
    class ChatGPTHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClientCustom> httpClients = new();
        private static readonly object objLock = new();
        private readonly OptionPageGridGeneral options;

        public string Proxy { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ChatGPTHttpClientFactory class.
        /// </summary>
        /// <param name="options">The options for configuring the HttpClient.</param>
        public ChatGPTHttpClientFactory(OptionPageGridGeneral options)
        {
            this.options = options;
        }

        /// <summary>
        /// Creates an HttpClient with the given name.
        /// </summary>
        /// <param name="name">The name of the HttpClient.</param>
        /// <returns>The created HttpClient.</returns>
        public HttpClient CreateClient(string name)
        {
            if (!httpClients.TryGetValue(name, out HttpClientCustom client))
            {
                lock (objLock)
                {
                    if (!httpClients.TryGetValue(name, out client))
                    {
                        client = CreateHttpClient(CreateMessageHandler());
                        httpClients.Add(name, client);
                    }
                }
            }

            if (client.IsDisposed)
            {
                httpClients.Remove(name);
                return CreateClient(name);
            }

            return client;
        }

        /// <summary>
        /// Sets the proxy for the HttpClient.
        /// </summary>
        /// <param name="proxy">The proxy to set.</param>
        public void SetProxy(string proxy)
        {
            if (proxy != Proxy)
            {
                Proxy = proxy;

                KeyValuePair<string, HttpClientCustom>[] list = httpClients.ToArray();

                foreach (KeyValuePair<string, HttpClientCustom> item in list)
                {
                    item.Value.Dispose();
                    httpClients.Remove(item.Key);
                }
            }
        }

        /// <summary>
        /// Creates an HttpClient with the specified message handler and default settings.
        /// </summary>
        /// <param name="handler">The message handler.</param>
        /// <returns>An HttpClient with the specified message handler and default settings.</returns>
        protected HttpClientCustom CreateHttpClient(HttpMessageHandler handler)
        {
            HttpClientCustom lookHttp = new(handler);

            lookHttp.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            lookHttp.DefaultRequestHeaders.Connection.Add("keep-alive");
            lookHttp.Timeout = new TimeSpan(0, 0, 120);

            return lookHttp;
        }

        /// <summary>
        /// Creates an HttpMessageHandler with the specified proxy settings.
        /// </summary>
        /// <returns>An HttpMessageHandler with the specified proxy settings.</returns>
        protected HttpMessageHandler CreateMessageHandler()
        {
            HttpClientHandler handler = new RequestCaptureHandler(options.LogRequests, options.LogResponses, options.UseVisualStudioIdentity)
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = true,
                AllowAutoRedirect = true,
                ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                    {
                        return true;
                    }

                    // Do not allow this client to communicate with unauthenticated servers.
                    return false;
                },
                MaxConnectionsPerServer = 256,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls
            };

            if (!string.IsNullOrWhiteSpace(Proxy))
            {
                handler.Proxy = new WebProxy(new Uri(Proxy));
            }

            return handler;
        }
    }
}
