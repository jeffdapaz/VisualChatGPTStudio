using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Embedding;
using OpenAI_API.Files;
using OpenAI_API.Images;
using OpenAI_API.Models;
using OpenAI_API.Moderation;
using System.Net.Http;

namespace OpenAI_API
{
    /// <summary>
    /// Entry point to the OpenAPI API, handling auth and allowing access to the various API endpoints
    /// </summary>
    public class OpenAIAPI : IOpenAIAPI
    {
        /// <summary>
        /// Base url for OpenAI
        /// for OpenAI, should be "https://api.openai.com/{0}/{1}"
        /// for Azure, should be "https://(your-resource-name.openai.azure.com/openai/deployments/(deployment-id)/{1}?api-version={0}"
        /// </summary>
        public string ApiUrlFormat { get; set; } = "https://api.openai.com/{0}/{1}";

        /// <summary>
        /// Version of the Rest Api
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// The API authentication information to use for API calls
        /// </summary>
        public APIAuthentication Auth { get; set; }

        /// <summary>
        /// Optionally provide an IHttpClientFactory to create the client to send requests.
        /// </summary>
        public IHttpClientFactory HttpClientFactory { get; set; }

        /// <summary>
        /// Creates a new entry point to the OpenAPI API, handling auth and allowing access to the various API endpoints
        /// </summary>
        /// <param name="apiKeys">The API authentication information to use for API calls, or <see langword="null"/> to attempt to use the <see cref="APIAuthentication.Default"/>, potentially loading from environment vars or from a config file.</param>
        public OpenAIAPI(APIAuthentication apiKeys = null)
        {
            this.Auth = apiKeys.ThisOrDefault();
            Completions = new CompletionEndpoint(this);
            Models = new ModelsEndpoint(this);
            Files = new FilesEndpoint(this);
            Embeddings = new EmbeddingEndpoint(this);
            Chat = new ChatEndpoint(this);
            Moderation = new ModerationEndpoint(this);
            ImageGenerations = new ImageGenerationEndpoint(this);
        }

        /// <summary>
        /// Creates an instance of the OpenAIAPI configured for Azure with the specified resource name and deployment ID.
        /// </summary>
        /// <param name="YourResourceName">The name of the Azure resource where the OpenAI service is deployed.</param>
        /// <param name="deploymentId">The ID of the specific deployment of the OpenAI model.</param>
        /// <param name="apiKey">The API authentication key used to access the OpenAI service.</param>
        /// <returns>
        /// An instance of the OpenAIAPI configured for Azure with the provided parameters.
        /// </returns>
        public static OpenAIAPI ForAzure(string YourResourceName, string deploymentId, APIAuthentication apiKey)
        {
            return new OpenAIAPI(apiKey)
            {
                ApiVersion = "2022-12-01",
                ApiUrlFormat = $"https://{YourResourceName}.openai.azure.com/openai/deployments/{deploymentId}/" + "{1}?api-version={0}"
            };
        }

        /// <summary>
        /// Creates an instance of the OpenAIAPI configured for Azure with the specified URL and API key.
        /// </summary>
        /// <param name="url">The endpoint URL for the Azure OpenAI service.</param>
        /// <param name="apiKey">The API authentication key used to access the service.</param>
        /// <returns>
        /// An instance of OpenAIAPI configured to communicate with the Azure service at the specified URL.
        /// </returns>
        public static OpenAIAPI ForAzure(string url, APIAuthentication apiKey)
        {
            return new OpenAIAPI(apiKey) { ApiVersion = string.Empty, ApiUrlFormat = url };
        }

        /// <summary>
        /// Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way you “program” the API to do a task is by simply describing the task in plain english or providing a few written examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
        /// </summary>
        public ICompletionEndpoint Completions { get; }

        /// <summary>
        /// The API lets you transform text into a vector (list) of floating point numbers. The distance between two vectors measures their relatedness. Small distances suggest high relatedness and large distances suggest low relatedness.
        /// </summary>
        public IEmbeddingEndpoint Embeddings { get; }

        /// <summary>
        /// Text generation in the form of chat messages. This interacts with the ChatGPT API.
        /// </summary>
        public IChatEndpoint Chat { get; }

        /// <summary>
        /// Classify text against the OpenAI Content Policy.
        /// </summary>
        public IModerationEndpoint Moderation { get; }

        /// <summary>
        /// The API endpoint for querying available Engines/models
        /// </summary>
        public IModelsEndpoint Models { get; }

        /// <summary>
        /// The API lets you do operations with files. You can upload, delete or retrieve files. Files can be used for fine-tuning, search, etc.
        /// </summary>
        public IFilesEndpoint Files { get; }

        /// <summary>
        /// The API lets you do operations with images. Given a prompt and/or an input image, the model will generate a new image.
        /// </summary>
        public IImageGenerationEndpoint ImageGenerations { get; }
    }
}
