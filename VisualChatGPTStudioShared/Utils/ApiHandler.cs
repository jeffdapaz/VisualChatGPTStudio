using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils.Http;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Static class containing methods for interacting with the API.
    /// </summary>
    static class ApiHandler
    {
        private static OpenAIAPI openAiAPI;
        private static OpenAIAPI azureAPI;
        private static ChatGPTHttpClientFactory chatGPTHttpClient;
        private static readonly TimeSpan timeout = new(0, 0, 120);

        #region Public Methods

        /// <summary>
        /// Asynchronously gets a completion response.
        /// </summary>
        /// <param name="options">The options for the chatbot.</param>
        /// <param name="systemMessage">The system message to send to the chatbot.</param>
        /// <param name="userInput">The user input to send to the chatbot.</param>
        /// <param name="stopSequences">The stop sequences to use for ending the conversation.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The response from the chatbot.</returns>
        public static async Task<string> GetCompletionResponseAsync(OptionPageGridGeneral options, string systemMessage, string userInput, string[] stopSequences, CancellationToken cancellationToken)
        {
            ICompletionEndpoint endpoint = CreateCompletionConversation(options);

            if (options.MinifyRequests)
            {
                userInput = TextFormat.MinifyText(userInput);
            }

            userInput = TextFormat.RemoveCharactersFromText(userInput, options.CharactersToRemoveFromRequests.Split(','));

            if (stopSequences != null && stopSequences.Length > 0)
            {
                endpoint.DefaultCompletionRequestArgs.MultipleStopSequences = stopSequences;
            }

            StringBuilder promptBuilder = new(systemMessage);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(userInput);

            Task<string> task = endpoint.GetCompletion(promptBuilder.ToString());

            await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);

            if (task.IsFaulted)
            {
                throw task.Exception.InnerException ?? task.Exception;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await task;
        }

        /// <summary>
        /// Asynchronously retrieves a response from a chatbot based on the provided options, system message, user input, and other parameters.
        /// </summary>
        /// <param name="options">The configuration options for the conversation.</param>
        /// <param name="systemMessage">The initial system message to set the context for the conversation.</param>
        /// <param name="userInput">The input provided by the user for the chatbot to respond to.</param>
        /// <param name="stopSequences">An array of sequences that will stop the chatbot's response generation.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <param name="image">An optional byte array representing an image to be included in the conversation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the chatbot's response as a string.</returns>
        public static async Task<string> GetResponseAsync(OptionPageGridGeneral options, string systemMessage, string userInput, string[] stopSequences, CancellationToken cancellationToken, byte[] image = null)
        {
            Conversation chat = CreateConversationForCompletions(options, systemMessage, userInput, stopSequences, image);

            string selectedContextFilesCode = await GetSelectedContextItemsCodeAsync();

            if (!string.IsNullOrWhiteSpace(selectedContextFilesCode))
            {
                chat.AppendSystemMessage(selectedContextFilesCode);
            }

            Task<string> task = chat.GetResponseContentAsync();

            await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);

            if (task.IsFaulted)
            {
                throw task.Exception.InnerException ?? task.Exception;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await task;
        }

        /// <summary>
        /// Asynchronously gets the response from the chatbot.
        /// </summary>
        /// <param name="options">The options for the chat.</param>
        /// <param name="systemMessage">The system message to display in the chat.</param>
        /// <param name="userInput">The user input in the chat.</param>
        /// <param name="stopSequences">The stop sequences to end the conversation.</param>
        /// <param name="resultHandler">The action to handle the chatbot response.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task GetResponseAsync(OptionPageGridGeneral options, string systemMessage, string userInput, string[] stopSequences, Action<string> resultHandler, CancellationToken cancellationToken)
        {
            Conversation chat = CreateConversationForCompletions(options, systemMessage, userInput, stopSequences);

            string selectedContextFilesCode = await GetSelectedContextItemsCodeAsync();

            if (!string.IsNullOrWhiteSpace(selectedContextFilesCode))
            {
                chat.AppendSystemMessage(selectedContextFilesCode);
            }

            Task task = chat.StreamResponseFromChatbotAsync(resultHandler);

            await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);

            if (task.IsFaulted)
            {
                throw task.Exception.InnerException ?? task.Exception;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Creates a conversation with the specified options and system message.
        /// </summary>
        /// <param name="options">The options to use for the conversation.</param>
        /// <param name="systemMessage">The system message to append to the conversation.</param>
        /// <returns>The created conversation.</returns>
        public static Conversation CreateConversation(OptionPageGridGeneral options, string systemMessage)
        {
            Conversation chat;

            if (options.Service == OpenAIService.OpenAI)
            {
                CreateOpenAIApiHandler(options);

                chat = new Conversation((ChatEndpoint)openAiAPI.Chat);
            }
            else
            {
                CreateAzureApiHandler(options);

                chat = new Conversation((ChatEndpoint)azureAPI.Chat);
            }

            chat.AppendSystemMessage(systemMessage);

            chat.AutoTruncateOnContextLengthExceeded = true;
            chat.RequestParameters.Temperature = options.Temperature;
            chat.RequestParameters.MaxTokens = options.MaxTokens;
            chat.RequestParameters.TopP = options.TopP;
            chat.RequestParameters.FrequencyPenalty = options.FrequencyPenalty;
            chat.RequestParameters.PresencePenalty = options.PresencePenalty;

            chat.Model = string.IsNullOrWhiteSpace(options.CustomModel) ? options.Model.GetStringValue() : options.CustomModel;

            return chat;
        }

        /// <summary>
        /// Creates a completion conversation with the specified options and system message.
        /// </summary>
        /// <param name="options">The options to use for the conversation.</param>
        /// <returns>The created conversation.</returns>
        public static ICompletionEndpoint CreateCompletionConversation(OptionPageGridGeneral options)
        {
            CreateOpenAIApiHandler(options);

            ICompletionEndpoint chat = openAiAPI.Completions;

            chat.DefaultCompletionRequestArgs.MaxTokens = options.CompletionMaxTokens ?? options.MaxTokens;
            chat.DefaultCompletionRequestArgs.Temperature = options.CompletionTemperature ?? options.Temperature;
            chat.DefaultCompletionRequestArgs.TopP = options.CompletionTopP ?? options.TopP;
            chat.DefaultCompletionRequestArgs.FrequencyPenalty = options.CompletionFrequencyPenalty ?? options.FrequencyPenalty;
            chat.DefaultCompletionRequestArgs.PresencePenalty = options.CompletionPresencePenalty ?? options.PresencePenalty;

            if (!string.IsNullOrWhiteSpace(options.CompletionCustomModel))
            {
                chat.DefaultCompletionRequestArgs.Model = options.CompletionCustomModel;
            }
            else if (!string.IsNullOrWhiteSpace(options.CustomModel))
            {
                chat.DefaultCompletionRequestArgs.Model = options.CustomModel;
            }
            else
            {
                chat.DefaultCompletionRequestArgs.Model = options.Model.GetStringValue();
            }

            return chat;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Creates a conversation object for handling completions based on the provided options, system message, user input, and optional image data.
        /// </summary>
        /// <param name="options">The options that configure the conversation settings.</param>
        /// <param name="systemMessage">The system message to initialize the conversation.</param>
        /// <param name="userInput">The input provided by the user for the conversation.</param>
        /// <param name="stopSequences">An array of stop sequences to control the conversation flow.</param>
        /// <param name="image">Optional byte array representing an image to be included in the conversation.</param>
        /// <returns>
        /// A <see cref="ConversationOverride"/> object that encapsulates the conversation details.
        /// </returns>
        private static Conversation CreateConversationForCompletions(OptionPageGridGeneral options, string systemMessage, string userInput, string[] stopSequences, byte[] image = null)
        {
            Conversation chat = CreateConversation(options, systemMessage);

            if (options.MinifyRequests)
            {
                userInput = TextFormat.MinifyText(userInput);
            }

            userInput = TextFormat.RemoveCharactersFromText(userInput, options.CharactersToRemoveFromRequests.Split(','));

            chat.AppendUserInput(userInput);

            if (image != null)
            {
                List<ChatContentForImage> chatContent = [new(image)];

                chat.AppendUserInput(chatContent);
            }

            if (stopSequences != null && stopSequences.Length > 0)
            {
                chat.RequestParameters.MultipleStopSequences = stopSequences;
            }

            return chat;
        }

        /// <summary>
        /// Creates an OpenAI API handler based on the provided options.
        /// </summary>
        /// <param name="options">The options to use for creating the OpenAI API handler.</param>
        private static void CreateOpenAIApiHandler(OptionPageGridGeneral options)
        {
            if (openAiAPI == null)
            {
                chatGPTHttpClient = new(options);

                if (!string.IsNullOrWhiteSpace(options.Proxy))
                {
                    chatGPTHttpClient.SetProxy(options.Proxy);
                }

                APIAuthentication auth;

                if (!string.IsNullOrWhiteSpace(options.OpenAIOrganization))
                {
                    auth = new(options.ApiKey, options.OpenAIOrganization);
                }
                else
                {
                    auth = new(options.ApiKey);
                }

                openAiAPI = new(auth);

                if (!string.IsNullOrWhiteSpace(options.BaseAPI))
                {
                    openAiAPI.ApiUrlFormat = options.BaseAPI + "/{0}/{1}";
                }

                openAiAPI.HttpClientFactory = chatGPTHttpClient;
            }
            else if (IsOptionsParametersModified(options))
            {
                openAiAPI = null;
                CreateOpenAIApiHandler(options);
            }

            if (openAiAPI.Auth.ApiKey != options.ApiKey)
            {
                openAiAPI.Auth.ApiKey = options.ApiKey;
            }

            if ((openAiAPI.Auth.OpenAIOrganization ?? string.Empty) != (options.OpenAIOrganization ?? string.Empty))
            {
                openAiAPI.Auth.OpenAIOrganization = options.OpenAIOrganization;
            }
        }

        /// <summary>
        /// Checks if the options parameters have been modified.
        /// </summary>
        /// <param name="options">The options page containing general settings.</param>
        /// <returns>
        /// True if settings have been modified; otherwise, false.
        /// </returns>
        private static bool IsOptionsParametersModified(OptionPageGridGeneral options)
        {
            return IsProxyModified(options) || IsBaseApiModified(options);
        }

        /// <summary>
        /// Checks if the proxy setting has been modified in the options.
        /// </summary>
        /// <param name="options">The general options page grid containing the proxy settings.</param>
        /// <returns>
        /// True if the proxy setting has been modified; otherwise, false.
        /// </returns>
        private static bool IsProxyModified(OptionPageGridGeneral options)
        {
            return (chatGPTHttpClient.Proxy ?? string.Empty) != (options.Proxy ?? string.Empty);
        }

        /// <summary>
        /// Checks if the base API URL has been modified from the default value.
        /// </summary>
        /// <param name="options">The general options containing the base API URL to check against.</param>
        /// <returns>
        /// True if the base API URL has been modified; otherwise, false.
        /// </returns>
        private static bool IsBaseApiModified(OptionPageGridGeneral options)
        {
            if (string.IsNullOrWhiteSpace(options.BaseAPI))
            {
                return openAiAPI.ApiUrlFormat != "https://api.openai.com/{0}/{1}";
            }

            return openAiAPI.ApiUrlFormat != options.BaseAPI + "/{0}/{1}";
        }

        /// <summary>
        /// Creates an Azure API handler based on the provided options. 
        /// </summary>
        /// <param name="options">The options to use for creating/updating the Azure API handler.</param>
        private static void CreateAzureApiHandler(OptionPageGridGeneral options)
        {
            if (azureAPI == null)
            {
                chatGPTHttpClient = new(options);

                if (!string.IsNullOrWhiteSpace(options.Proxy))
                {
                    chatGPTHttpClient.SetProxy(options.Proxy);
                }

                if (!string.IsNullOrWhiteSpace(options.AzureUrlOverride))
                {
                    azureAPI = OpenAIAPI.ForAzure(options.AzureUrlOverride, options.ApiKey);
                }
                else
                {
                    azureAPI = OpenAIAPI.ForAzure(options.AzureResourceName, options.AzureDeploymentId, options.ApiKey);
                }

                azureAPI.HttpClientFactory = chatGPTHttpClient;
            }
            else if ((chatGPTHttpClient.Proxy ?? string.Empty) != (options.Proxy ?? string.Empty) ||
                    (string.IsNullOrWhiteSpace(options.AzureUrlOverride) &&
                    (
                        !azureAPI.ApiUrlFormat.ToLower().Contains(options.AzureResourceName.ToLower()) ||
                        !azureAPI.ApiUrlFormat.ToLower().Contains(options.AzureDeploymentId.ToLower()))
                    ))
            {
                azureAPI = null;
                CreateAzureApiHandler(options);
            }

            if (azureAPI.Auth.ApiKey != options.ApiKey)
            {
                azureAPI.Auth.ApiKey = options.ApiKey;
            }

            if (!string.IsNullOrWhiteSpace(options.AzureUrlOverride))
            {
                if (azureAPI.ApiUrlFormat != options.AzureUrlOverride)
                {
                    azureAPI.ApiUrlFormat = options.AzureUrlOverride;
                }
            }
            else
            {
                if ((azureAPI.ApiVersion ?? string.Empty) != (options.AzureApiVersion ?? string.Empty))
                {
                    azureAPI.ApiVersion = options.AzureApiVersion;
                }
            }
        }

        /// <summary>
        /// Asynchronously gets the code of the selected context items.
        /// </summary>  
        /// <returns>The code of the selected context items as a string.</returns>
        private static async Task<string> GetSelectedContextItemsCodeAsync()
        {
            StringBuilder result = new();

            List<string> selectedContextFilesCode = await TerminalWindowSolutionContextCommand.Instance.GetSelectedContextItemsCodeAsync();

            foreach (string code in selectedContextFilesCode)
            {
                result.AppendLine(code);
            }

            return result.ToString();
        }

        #endregion Private Methods
    }

    /// <summary>
    /// Enum containing the different types of model languages.
    /// </summary>
    public enum ModelLanguageEnum
    {
        [EnumStringValue("gpt-3.5-turbo")]
        GPT_3_5_Turbo,
        [EnumStringValue("gpt-3.5-turbo-1106")]
        GPT_3_5_Turbo_1106,
        [EnumStringValue("gpt-4")]
        GPT_4,
        [EnumStringValue("gpt-4-32k")]
        GPT_4_32K,
        [EnumStringValue("gpt-4-turbo")]
        GPT_4_Turbo,
        [EnumStringValue("gpt-4o")]
        GPT_4o,
        [EnumStringValue("gpt-4o-mini")]
        GPT_4o_Mini,
        [EnumStringValue("o1")]
        o1,
        [EnumStringValue("o3-mini")]
        o3_mini
    }

    /// <summary>
    /// Enum to represent the different OpenAI services available.
    /// </summary>
    public enum OpenAIService
    {
        OpenAI,
        AzureOpenAI
    }
}
