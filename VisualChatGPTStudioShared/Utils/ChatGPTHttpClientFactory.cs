using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using VisualChatGPTStudioShared.Utils;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// This class provides a factory for creating HttpClient instances for use with the ChatGPT API.
    /// </summary>
    class ChatGPTHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClientCustom> httpClients = new();
        private static readonly object objLock = new();
        private string m_proxy;

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
                        client = CreateHttpClient(CreateMessageHandler(m_proxy));
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
            if (proxy != m_proxy)
            {
                m_proxy = proxy;
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
        protected static HttpClientCustom CreateHttpClient(HttpMessageHandler handler)
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
        /// <param name="proxy">The proxy settings to use.</param>
        /// <returns>An HttpMessageHandler with the specified proxy settings.</returns>
        protected static HttpMessageHandler CreateMessageHandler(string proxy = null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            handler.UseCookies = true;
            handler.AllowAutoRedirect = true;
            handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
            handler.MaxConnectionsPerServer = 256;
            handler.SslProtocols =
                System.Security.Authentication.SslProtocols.Tls12 |
                System.Security.Authentication.SslProtocols.Tls11 |
                System.Security.Authentication.SslProtocols.Tls;

            if (!string.IsNullOrEmpty(proxy))
            {
                handler.Proxy = new WebProxy(new Uri(proxy));
            }

            return handler;
        }
    }
}
