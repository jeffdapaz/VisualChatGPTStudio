using System.Net.Http;

namespace JeffPires.VisualChatGPTStudio.Utils.Http
{
    /// <summary>
    /// This class is a custom implementation of the HttpClient class.
    /// </summary>
    class HttpClientCustom : HttpClient
    {
        /// <summary>
        /// Checks if the object has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the HttpClientCustom class with the specified HttpMessageHandler and streaming flag.
        /// </summary>
        /// <param name="handler">The HttpMessageHandler to use for sending HTTP requests.</param>
        public HttpClientCustom(HttpMessageHandler handler) : base(handler) { }

        /// <summary>
        /// Disposes the object and releases any associated resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to dispose managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
