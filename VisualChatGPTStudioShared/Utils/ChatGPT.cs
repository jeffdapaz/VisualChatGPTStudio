﻿using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils.Http;
using OpenAI_API;
using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Static class containing methods for interacting with the ChatGPT API.
    /// </summary>
    static class ChatGPT
    {
        private static OpenAIAPI openAiAPI;
        private static OpenAIAPI azureAPI;
        private static ChatGPTHttpClientFactory chatGPTHttpClient;
        private static readonly TimeSpan timeout = new(0, 0, 120);

        /// <summary>
        /// Asynchronously gets a response from a chatbot.
        /// </summary>
        /// <param name="options">The options for the chatbot.</param>
        /// <param name="systemMessage">The system message to send to the chatbot.</param>
        /// <param name="userInput">The user input to send to the chatbot.</param>
        /// <param name="stopSequences">The stop sequences to use for ending the conversation.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The response from the chatbot.</returns>
        public static async Task<string> GetResponseAsync(OptionPageGridGeneral options, string systemMessage, string userInput, string[] stopSequences, CancellationToken cancellationToken)
        {
            Conversation chat = CreateConversationForCompletions(options, systemMessage, userInput, stopSequences);

            string selectedContextFilesCode = await GetSelectedContextItemsCodeAsync();

            if (!string.IsNullOrWhiteSpace(selectedContextFilesCode))
            {
                chat.AppendSystemMessage(selectedContextFilesCode);
            }

            Task<string> task = chat.GetResponseFromChatbotAsync();

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

                chat = openAiAPI.Chat.CreateConversation();
            }
            else
            {
                CreateAzureApiHandler(options);

                chat = azureAPI.Chat.CreateConversation();
            }

            chat.AppendSystemMessage(systemMessage);

            chat.AutoTruncateOnContextLengthExceeded = true;
            chat.RequestParameters.Temperature = options.Temperature;
            chat.RequestParameters.MaxTokens = options.MaxTokens;
            chat.RequestParameters.TopP = options.TopP;
            chat.RequestParameters.FrequencyPenalty = options.FrequencyPenalty;
            chat.RequestParameters.PresencePenalty = options.PresencePenalty;

            chat.Model = string.IsNullOrEmpty(options.CustomModel) ? options.Model.GetStringValue() : options.CustomModel;

            return chat;
        }

        /// <summary>
        /// Creates a conversation for Completions with the given parameters.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="systemMessage">The system message.</param>
        /// <param name="userInput">The user input.</param>
        /// <param name="stopSequences">The stop sequences.</param>
        /// <returns>
        /// The created conversation.
        /// </returns>
        private static Conversation CreateConversationForCompletions(OptionPageGridGeneral options, string systemMessage, string userInput, string[] stopSequences)
        {
            Conversation chat = CreateConversation(options, systemMessage);

            if (options.MinifyRequests)
            {
                userInput = TextFormat.MinifyText(userInput);
            }

            userInput = TextFormat.RemoveCharactersFromText(userInput, options.CharactersToRemoveFromRequests.Split(','));

            chat.AppendUserInput(userInput);

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
            else if ((chatGPTHttpClient.Proxy ?? string.Empty) != (options.Proxy ?? string.Empty) || (!string.IsNullOrWhiteSpace(options.BaseAPI) && !openAiAPI.ApiUrlFormat.StartsWith(options.BaseAPI)))
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

                azureAPI = OpenAIAPI.ForAzure(options.AzureResourceName, options.AzureDeploymentId, options.ApiKey);

                azureAPI.HttpClientFactory = chatGPTHttpClient;
            }
            else if ((chatGPTHttpClient.Proxy ?? string.Empty) != (options.Proxy ?? string.Empty) || !azureAPI.ApiUrlFormat.Contains(options.AzureResourceName) || !azureAPI.ApiUrlFormat.Contains(options.AzureDeploymentId))
            {
                azureAPI = null;
                CreateAzureApiHandler(options);
            }

            if (azureAPI.Auth.ApiKey != options.ApiKey)
            {
                azureAPI.Auth.ApiKey = options.ApiKey;
            }

            if ((azureAPI.ApiVersion ?? string.Empty) != (options.AzureApiVersion ?? string.Empty))
            {
                azureAPI.ApiVersion = options.AzureApiVersion;
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
        [EnumStringValue("gpt-4-turbo-preview")]
        GPT_4_Turbo
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
