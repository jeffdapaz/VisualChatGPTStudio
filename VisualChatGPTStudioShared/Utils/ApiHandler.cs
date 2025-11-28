using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils.Http;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.ResponsesAPI;
using OpenAI_API.ResponsesAPI.Models.Request;
using OpenAI_API.ResponsesAPI.Models.Response;

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
                userInput = TextFormat.MinifyText(userInput, " ");
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
        /// <param name="modelOrAzureDeploymentOverride">An optional parameter to specify the model name or Azure deployment name to override the option's parameters.</param>
        /// <returns>A task that represents the asynchronous operation, containing the chatbot's response as a string.</returns>
        public static async Task<string> GetResponseAsync(OptionPageGridGeneral options,
                                                          string systemMessage,
                                                          string userInput,
                                                          string[] stopSequences,
                                                          CancellationToken cancellationToken,
                                                          byte[] image = null,
                                                          string modelOrAzureDeploymentOverride = "")
        {
            Conversation chat = CreateConversationForCompletions(options, systemMessage, userInput, stopSequences, image, modelOrAzureDeploymentOverride);

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

            Task task = chat.StreamResponseFromChatbotAsync(resultHandler, cancellationToken);

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
        /// <param name="modelOrAzureDeploymentOverride">An optional parameter to specify the model name or Azure deployment name to override the option's parameters.</param>
        /// <returns>The created conversation.</returns>
        public static Conversation CreateConversation(OptionPageGridGeneral options, string systemMessage, string modelOrAzureDeploymentOverride = "")
        {
            Conversation chat;

            if (options.Service == OpenAIService.OpenAI)
            {
                CreateOpenAIApiHandler(options);

                chat = new Conversation((ChatEndpoint)openAiAPI.Chat)
                {
                    Model = !string.IsNullOrWhiteSpace(modelOrAzureDeploymentOverride) ? modelOrAzureDeploymentOverride : options.Model
                };
            }
            else
            {
                CreateAzureApiHandler(options, modelOrAzureDeploymentOverride);

                chat = new Conversation((ChatEndpoint)azureAPI.Chat);
            }

            chat.AppendSystemMessage(systemMessage);

            chat.AutoTruncateOnContextLengthExceeded = true;
            chat.RequestParameters.Temperature = options.Temperature;
            chat.RequestParameters.MaxTokens = options.MaxTokens;
            chat.RequestParameters.TopP = options.TopP;
            chat.RequestParameters.FrequencyPenalty = options.FrequencyPenalty;
            chat.RequestParameters.PresencePenalty = options.PresencePenalty;

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

            if (!string.IsNullOrWhiteSpace(options.CompletionBaseAPI))
            {
                openAiAPI.ApiUrlFormat = options.CompletionBaseAPI + "/{0}/{1}";
            }

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
            else
            {
                chat.DefaultCompletionRequestArgs.Model = options.Model;
            }

            return chat;
        }

        /// <summary>
        /// Asynchronously creates and sends a computer use request based on the provided options, prompt, display dimensions, and screenshot.
        /// </summary>
        /// <param name="options">The options containing settings for request formatting and other configurations.</param>
        /// <param name="prompt">The text prompt to include in the request.</param>
        /// <param name="displayWidth">The width of the display to include in the tool configuration.</param>
        /// <param name="displayHeight">The height of the display to include in the tool configuration.</param>
        /// <param name="screenshot">A byte array representing the screenshot to include in the request content.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The task result contains the <see cref="ComputerUseResponse"/> returned from sending the request.</returns>
        public static async Task<ComputerUseResponse> GetComputerUseResponseAsync(OptionPageGridGeneral options,
                                                                                  string prompt,
                                                                                  int displayWidth,
                                                                                  int displayHeight,
                                                                                  byte[] screenshot,
                                                                                  CancellationToken cancellationToken)
        {
            if (options.MinifyRequests)
            {
                prompt = TextFormat.MinifyText(prompt, " ");
            }

            prompt = TextFormat.RemoveCharactersFromText(prompt, options.CharactersToRemoveFromRequests.Split(','));

            ComputerUseTool tool = new()
            {
                DisplayWidth = displayWidth,
                DisplayHeight = displayHeight
            };

            List<ComputerUseInput> inputList =
            [
                new ComputerUseInput
                {
                    Role = "user",
                    Content = [new ComputerUseContent(prompt), new ComputerUseContent(screenshot)]
                }
            ];

            ComputerUseRequest request = new()
            {
                Tools = [tool],
                Input = inputList
            };

            return await SendComputerUseRequestAsync(options, request, cancellationToken);
        }

        /// <summary>
        /// Creates and sends a computer use request based on the provided options and prompt,
        /// applying text modifications and display settings, then returns the response.
        /// </summary>
        /// <param name="options">The options containing settings for request minification and character removal.</param>
        /// <param name="prompt">The input prompt text to be processed and sent.</param>
        /// <param name="displayWidth">The width of the display for the computer use tool.</param>
        /// <param name="displayHeight">The height of the display for the computer use tool.</param>
        /// <param name="previousResponseId">The identifier of the previous response to maintain context.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, with a ComputerUseResponse result.</returns>
        public static async Task<ComputerUseResponse> GetComputerUseResponseAsync(OptionPageGridGeneral options,
                                                                                          string prompt,
                                                                                          int displayWidth,
                                                                                          int displayHeight,
                                                                                          string previousResponseId,
                                                                                          CancellationToken cancellationToken)
        {
            if (options.MinifyRequests)
            {
                prompt = TextFormat.MinifyText(prompt, " ");
            }

            prompt = TextFormat.RemoveCharactersFromText(prompt, options.CharactersToRemoveFromRequests.Split(','));

            ComputerUseTool tool = new()
            {
                DisplayWidth = displayWidth,
                DisplayHeight = displayHeight
            };

            List<ComputerUseInput> inputList =
            [
                new ComputerUseInput
                {
                    Role = "user",
                    Content = [new ComputerUseContent(prompt)]
                }
            ];

            ComputerUseRequest request = new()
            {
                Tools = [tool],
                Input = inputList,
                PreviousResponseId = previousResponseId
            };

            return await SendComputerUseRequestAsync(options, request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously creates and sends a computer use request with the specified display dimensions, screenshot, and related identifiers.
        /// </summary>
        /// <param name="options">The options for the request configuration.</param>
        /// <param name="displayWidth">The width of the display.</param>
        /// <param name="displayHeight">The height of the display.</param>
        /// <param name="screenshot">A byte array representing the screenshot to include in the request.</param>
        /// <param name="lastCallId">The identifier of the last call to associate with the input.</param>
        /// <param name="previousResponseId">The identifier of the previous response to link the request.</param>
        /// <param name="acknowledgeSafetyChecks">A list of safety checks to acknowledge.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ComputerUseResponse"/> returned from the request.</returns>
        public static async Task<ComputerUseResponse> GetComputerUseResponseAsync(OptionPageGridGeneral options,
                                                                                  int displayWidth,
                                                                                  int displayHeight,
                                                                                  byte[] screenshot,
                                                                                  string lastCallId,
                                                                                  string previousResponseId,
                                                                                  List<ComputerUseSafetyCheck> acknowledgeSafetyChecks,
                                                                                  CancellationToken cancellationToken)
        {
            ComputerUseTool tool = new()
            {
                DisplayWidth = displayWidth,
                DisplayHeight = displayHeight
            };

            List<ComputerUseInput> inputList =
            [
                new ComputerUseInput
                {
                    CallId = lastCallId,
                    Type = "computer_call_output",
                    Output = new ComputerUseContent(screenshot),
                    AcknowledgedSafetyChecks = acknowledgeSafetyChecks
                }
            ];

            ComputerUseRequest request = new()
            {
                Tools = [tool],
                Input = inputList,
                PreviousResponseId = previousResponseId
            };

            return await SendComputerUseRequestAsync(options, request, cancellationToken);
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
        /// <param name="modelOrAzureDeploymentOverride">An optional parameter to specify the model name or Azure deployment name to override the option's parameters.</param>
        /// <returns>
        /// A <see cref="ConversationOverride"/> object that encapsulates the conversation details.
        /// </returns>
        private static Conversation CreateConversationForCompletions(OptionPageGridGeneral options,
                                                                     string systemMessage,
                                                                     string userInput,
                                                                     string[] stopSequences,
                                                                     byte[] image = null,
                                                                     string modelOrAzureDeploymentOverride = "")
        {
            Conversation chat = CreateConversation(options, systemMessage, modelOrAzureDeploymentOverride);

            if (options.MinifyRequests)
            {
                userInput = TextFormat.MinifyText(userInput, " ");
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
                    openAiAPI.ApiUrlFormat = options.BaseAPI.TrimEnd() + "/{0}/{1}";
                }

                if (!string.IsNullOrWhiteSpace(options.ApiVersion))
                {
                    openAiAPI.ApiVersion = options.ApiVersion;
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
        /// <param name="modelOrAzureDeploymentOverride">An optional parameter to specify the deployment name to override the option's parameters.
        private static void CreateAzureApiHandler(OptionPageGridGeneral options, string deploymentOverride = "")
        {
            string deployment = !string.IsNullOrWhiteSpace(deploymentOverride) ? deploymentOverride : options.AzureDeploymentId;

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
                    azureAPI = OpenAIAPI.ForAzure(options.AzureResourceName, deployment, options.ApiKey);
                }

                azureAPI.HttpClientFactory = chatGPTHttpClient;
            }
            else if ((chatGPTHttpClient.Proxy ?? string.Empty) != (options.Proxy ?? string.Empty) ||
                    (string.IsNullOrWhiteSpace(options.AzureUrlOverride) &&
                    (
                        !azureAPI.ApiUrlFormat.ToLower().Contains(options.AzureResourceName.ToLower()) ||
                        !azureAPI.ApiUrlFormat.ToLower().Contains(deployment.ToLower()))
                    ))
            {
                azureAPI = null;
                CreateAzureApiHandler(options, deploymentOverride);
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

        /// <summary>
        /// Sends an asynchronous request to the computer use API endpoint based on the provided options and request data.
        /// Determines the appropriate endpoint URL depending on whether the service is OpenAI or Azure,
        /// configures the HTTP client including proxy settings if specified, and then sends the request.
        /// </summary>
        /// <param name="options">The configuration options including service type, API keys, URLs, and proxy settings.</param>
        /// <param name="request">The computer use request payload to be sent.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>Containing the response from the computer use API.</returns>
        private static async Task<ComputerUseResponse> SendComputerUseRequestAsync(OptionPageGridGeneral options, ComputerUseRequest request, CancellationToken cancellationToken)
        {
            string endpointUrl;
            bool isAzure = false;

            if (options.Service == OpenAIService.OpenAI)
            {
                endpointUrl = !string.IsNullOrWhiteSpace(options.BaseAPI)
                    ? $"{options.BaseAPI.TrimEnd('/')}/responses"
                    : "https://api.openai.com/v1/responses";
            }
            else
            {
                endpointUrl = !string.IsNullOrWhiteSpace(options.AzureUrlOverrideForComputerUse)
                    ? options.AzureUrlOverrideForComputerUse
                    : $"https://{options.AzureResourceName}.openai.azure.com/openai/v1/responses?api-version={options.AzureApiVersionForComputerUse}";
                isAzure = true;
            }

            ChatGPTHttpClientFactory chatGPTHttpClient = new(options);

            if (!string.IsNullOrWhiteSpace(options.Proxy))
            {
                chatGPTHttpClient.SetProxy(options.Proxy);
            }

            HttpClient httpClient = chatGPTHttpClient.CreateClient();
            httpClient.BaseAddress = new Uri(endpointUrl);

            return await ResponsesApiHandler.SendComputerUseRequestAsync(
                request,
                httpClient,
                options.ApiKey,
                isAzure,
                options.OpenAIOrganization,
                cancellationToken
            );
        }

        #endregion Private Methods
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
