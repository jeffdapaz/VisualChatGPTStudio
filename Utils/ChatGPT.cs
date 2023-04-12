using JeffPires.VisualChatGPTStudio.Options;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Static class containing methods for interacting with the ChatGPT API.
    /// </summary>
    static class ChatGPT
    {
        private static OpenAIAPI api;

        /// <summary>
        /// Requests a completion from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request to send to the API.</param>
        /// <returns>The completion result.</returns>
        public static async Task<CompletionResult> RequestAsync(OptionPageGridGeneral options, string request)
        {
            CreateApiHandler(options.ApiKey, options.Proxy);

            return await api.Completions.CreateCompletionAsync(GetRequest(options, request));
        }

        /// <summary>
        /// Requests a completion from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request to send to the API.</param>
        /// <param name="stopSequences">Up to 4 sequences where the API will stop generating further tokens.</param>
        /// <returns>The completion result.</returns>
        public static async Task<CompletionResult> RequestAsync(OptionPageGridGeneral options, string request, string[] stopSequences)
        {
            CreateApiHandler(options.ApiKey, options.Proxy);

            return await api.Completions.CreateCompletionAsync(GetRequest(options, request, stopSequences));
        }

        /// <summary>
        /// Requests a completion from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request to send to the API.</param>
        /// <param name="resultHandler">The action to take when the result is received.</param>
        public static async Task RequestAsync(OptionPageGridGeneral options, string request, Action<int, CompletionResult> resultHandler)
        {
            CreateApiHandler(options.ApiKey, options.Proxy);

            await api.Completions.StreamCompletionAsync(GetRequest(options, request), resultHandler);
        }

        /// <summary>
        /// Requests a completion from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request to send to the API.</param>
        /// <param name="resultHandler">The action to take when the result is received.</param>
        /// <param name="stopSequences">Up to 4 sequences where the API will stop generating further tokens.</param>
        public static async Task RequestAsync(OptionPageGridGeneral options, string request, Action<int, CompletionResult> resultHandler, string[] stopSequences)
        {
            CreateApiHandler(options.ApiKey, options.Proxy);

            await api.Completions.StreamCompletionAsync(GetRequest(options, request, stopSequences), resultHandler);
        }

        /// <summary>
        /// Creates a new conversation and appends a system message with the specified TurboChatBehavior.
        /// </summary>
        /// <param name="options">The options to use for the conversation.</param>
        /// <returns>The newly created conversation.</returns>
        public static Conversation CreateConversation(OptionPageGridGeneral options)
        {
            CreateApiHandler(options.ApiKey, options.Proxy);

            Conversation chat = api.Chat.CreateConversation();

            chat.AppendSystemMessage(options.TurboChatBehavior);

            return chat;
        }

        /// <summary>
        /// Creates an API handler with the given API key and proxy.
        /// </summary>
        /// <param name="apiKey">The API key to use.</param>
        /// <param name="proxy">The proxy to use.</param>
        private static void CreateApiHandler(string apiKey, string proxy)
        {
            if (api == null)
            {
                api = new(apiKey);
            }
            else if (api.Auth.ApiKey != apiKey)
            {
                api.Auth.ApiKey = apiKey;
            }

            if (!string.IsNullOrWhiteSpace(proxy))
            {
                api.ApiUrlFormat = proxy + "/{0}/{1}";
            }
        }

        /// <summary>
        /// Gets a CompletionRequest object based on the given options and request.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request string.</param>
        /// <returns>A CompletionRequest object.</returns>
        private static CompletionRequest GetRequest(OptionPageGridGeneral options, string request)
        {
            return GetRequest(options, request, null);
        }

        /// <summary>
        /// Gets a CompletionRequest object based on the given options and request.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request string.</param>
        /// <param name="stopSequences">Up to 4 sequences where the API will stop generating further tokens.</param>
        /// <returns>A CompletionRequest object.</returns>
        private static CompletionRequest GetRequest(OptionPageGridGeneral options, string request, string[] stopSequences)
        {
            Model model = Model.DavinciText;

            switch (options.Model)
            {
                case ModelLanguageEnum.TextCurie001:
                    model = Model.CurieText;
                    break;
                case ModelLanguageEnum.TextBabbage001:
                    model = Model.BabbageText;
                    break;
                case ModelLanguageEnum.TextAda001:
                    model = Model.AdaText;
                    break;
                case ModelLanguageEnum.CodeDavinci:
                    model = Model.DavinciCode;
                    break;
                case ModelLanguageEnum.CodeCushman:
                    model = Model.CushmanCode;
                    break;
            }

            if (stopSequences == null || stopSequences.Length == 0)
            {
                stopSequences = options.StopSequences.Split(',');
            }

            return new(request, model, options.MaxTokens, options.Temperature, presencePenalty: options.PresencePenalty, frequencyPenalty: options.FrequencyPenalty, top_p: options.TopP, stopSequences: stopSequences);
        }
    }
}
