using System.Net.Http;

namespace VisualChatGPTStudioShared.Utils
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
        /// Default constructor.
        /// </summary>
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
