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
        private static OpenAIAPI apiForAzureTurboChat;
        private static ChatGPTHttpClientFactory chatGPTHttpClient;

        /// <summary>
        /// Requests a completion from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request to send to the API.</param>
        /// <returns>The completion result.</returns>
        public static async Task<CompletionResult> RequestAsync(OptionPageGridGeneral options, string request)
        {
            CreateApiHandler(options);

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
            CreateApiHandler(options);

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
            CreateApiHandler(options);

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
            CreateApiHandler(options);

            await api.Completions.StreamCompletionAsync(GetRequest(options, request, stopSequences), resultHandler);
        }

        /// <summary>
        /// Creates a new conversation and appends a system message with the specified TurboChatBehavior.
        /// </summary>
        /// <param name="options">The options to use for the conversation.</param>
        /// <returns>The newly created conversation.</returns>
        public static Conversation CreateConversation(OptionPageGridGeneral options)
        {
            Conversation chat;

            if (options.Service == OpenAIService.OpenAI || string.IsNullOrWhiteSpace(options.AzureTurboChatDeploymentId))
            {
                CreateApiHandler(options);

                chat = api.Chat.CreateConversation();
            }
            else
            {
                CreateApiHandlerForAzureTurboChat(options);

                chat = apiForAzureTurboChat.Chat.CreateConversation();
            }

            chat.AppendSystemMessage(options.TurboChatBehavior);

            if (options.TurboChatModelLanguage == TurboChatModelLanguageEnum.GPT_4)
            {
                chat.Model = Model.GPT4;
            }

            return chat;
        }

        /// <summary>
        /// Creates an API handler with the given API key and proxy.
        /// </summary>
        /// <param name="options">All configurations to create the connection</param>
        private static void CreateApiHandler(OptionPageGridGeneral options)
        {
            if (api == null)
            {
                chatGPTHttpClient = new();

                if (!string.IsNullOrWhiteSpace(options.Proxy))
                {
                    chatGPTHttpClient.SetProxy(options.Proxy);
                }

                if (options.Service == OpenAIService.AzureOpenAI)
                {
                    api = OpenAIAPI.ForAzure(options.AzureResourceName, options.AzureDeploymentId, options.ApiKey);
                }
                else
                {
                    APIAuthentication auth;

                    if (!string.IsNullOrWhiteSpace(options.OpenAIOrganization))
                    {
                        auth = new(options.ApiKey, options.OpenAIOrganization);
                    }
                    else
                    {
                        auth = new(options.ApiKey);
                    }

                    api = new(auth);
                }

                api.HttpClientFactory = chatGPTHttpClient;
            }
            else if ((options.Service == OpenAIService.AzureOpenAI && !api.ApiUrlFormat.ToUpper().Contains("AZURE")) || (options.Service == OpenAIService.OpenAI && api.ApiUrlFormat.ToUpper().Contains("AZURE")))
            {
                api = null;
                CreateApiHandler(options);
            }
            else if (api.Auth.ApiKey != options.ApiKey)
            {
                api.Auth.ApiKey = options.ApiKey;
            }
        }

        /// <summary>
        /// Creates an API handler for Azure TurboChat using the provided options.
        /// </summary>
        /// <param name="options">The options to use for creating the API handler.</param>
        private static void CreateApiHandlerForAzureTurboChat(OptionPageGridGeneral options)
        {
            if (apiForAzureTurboChat == null)
            {
                chatGPTHttpClient = new();

                if (!string.IsNullOrWhiteSpace(options.Proxy))
                {
                    chatGPTHttpClient.SetProxy(options.Proxy);
                }

                apiForAzureTurboChat = OpenAIAPI.ForAzure(options.AzureResourceName, options.AzureTurboChatDeploymentId, options.ApiKey);

                apiForAzureTurboChat.HttpClientFactory = chatGPTHttpClient;

                if (!string.IsNullOrWhiteSpace(options.AzureTurboChatApiVersion))
                {
                    apiForAzureTurboChat.ApiVersion = options.AzureTurboChatApiVersion;
                }
            }
            else if (apiForAzureTurboChat.Auth.ApiKey != options.ApiKey)
            {
                apiForAzureTurboChat.Auth.ApiKey = options.ApiKey;
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
            }

            if (stopSequences == null || stopSequences.Length == 0)
            {
                stopSequences = options.StopSequences.Split(',');
            }

            return new(request, model, options.MaxTokens, options.Temperature, presencePenalty: options.PresencePenalty, frequencyPenalty: options.FrequencyPenalty, top_p: options.TopP, stopSequences: stopSequences);
        }
    }
}
