using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Utils.API
{
    /// <summary>
    /// Conversation override class. Necessary to fix the "twice request" bug from the original code. 
    /// </summary>
    public class ConversationOverride : Conversation
    {
        #region Properties

        private readonly ChatEndpoint endpoint;

        public new ChatResult MostRecentApiResult { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ConversationOverride class with the specified endpoint, model and default chat request arguments.
        /// </summary>
        /// <param name="endpoint">The chat endpoint to use for the conversation.</param>
        /// <param name="model">The model to use for the conversation. Can be null.</param>
        /// <param name="defaultChatRequestArgs">The default chat request arguments to use for the conversation. Can be null.</param>
        public ConversationOverride(ChatEndpoint endpoint, Model model = null, ChatRequest defaultChatRequestArgs = null) : base(endpoint, model, defaultChatRequestArgs)
        {
            this.endpoint = endpoint;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Appends user input as a new chat message to the conversation.
        /// </summary>
        /// <param name="content">The content of the user input to be appended.</param>
        public void AppendUserInput(object content) => this.AppendMessage(new ChatMessageOverride(ChatMessageRole.User, content));

        /// <summary>
        /// Sends a request to the chatbot endpoint with the current set of messages and request parameters, and returns the response message content.
        /// </summary>
        /// <returns>
        /// The content of the response message from the chatbot endpoint, or null if an error occurred or no response was received.
        /// </returns>
        public new async Task<string> GetResponseFromChatbotAsync()
        {
            try
            {
                ChatRequest req = new(RequestParameters) { Messages = Messages.ToList() };

                ChatResult res = await endpoint.CreateChatCompletionAsync(req);

                MostRecentApiResult = res;

                if (res.Choices.Count > 0)
                {
                    ChatMessage newMsg = res.Choices[0].Message;

                    AppendMessage(newMsg);

                    return newMsg.Content;
                }
            }
            catch (HttpRequestException ex)
            {
                if (TruncateContextWhenExceeded(ex))
                {
                    return await GetResponseFromChatbotAsync();
                }
            }

            return null;
        }

        /// <summary>
        /// Streams the response from the chatbot asynchronously and invokes the provided result handler for each response received.
        /// </summary>
        /// <param name="resultHandler">The action to be invoked for each response received from the chatbot.</param>
        public new async Task StreamResponseFromChatbotAsync(Action<string> resultHandler)
        {
            await foreach (string res in StreamResponseEnumerableFromChatbotAsync())
            {
                resultHandler(res);
            }
        }

        /// <summary>
        /// Asynchronously streams the response from the chatbot as an enumerable of strings.
        /// </summary>
        /// <returns>
        /// An asynchronous enumerable of strings representing the response from the chatbot.
        /// </returns>
        public new async IAsyncEnumerable<string> StreamResponseEnumerableFromChatbotAsync()
        {
            ChatRequest request;

            StringBuilder responseStringBuilder = new();
            ChatMessageRole responseRole = null;

            IAsyncEnumerable<ChatResult> resStream = null;

            bool retrying = true;
            bool streamError = false;
            ChatResult firstStreamedResult;
            IAsyncEnumerator<ChatResult> enumerator = null;

            while (retrying)
            {
                retrying = false;
                request = new(RequestParameters) { Messages = Messages.ToList() };

                try
                {
                    resStream = endpoint.StreamChatEnumerableAsync(request);
                    enumerator = resStream.GetAsyncEnumerator();
                    await enumerator.MoveNextAsync();
                    firstStreamedResult = enumerator.Current;
                }
                catch (HttpRequestException ex)
                {
                    retrying = TruncateContextWhenExceeded(ex);
                }
                catch (ArgumentException)
                {
                    streamError = true;
                }
            }

            //In case of an error reading the stream, it returns a single response.
            if (streamError)
            {
                yield return await GetResponseFromChatbotAsync();

                yield break;
            }

            if (resStream == null)
            {
                throw new Exception("The chat result stream is null, but it shouldn't be");
            }

            do
            {
                ChatResult res = enumerator.Current;

                if (res.Choices.FirstOrDefault()?.Delta is ChatMessage delta)
                {
                    if (delta.Role != null)
                    {
                        responseRole = delta.Role;
                    }

                    string deltaTextContent = delta.Content;

                    if (!string.IsNullOrWhiteSpace(deltaTextContent))
                    {
                        responseStringBuilder.Append(deltaTextContent);

                        yield return deltaTextContent;
                    }
                }

                MostRecentApiResult = res;

            } while (await enumerator.MoveNextAsync());

            if (responseRole != null)
            {
                AppendMessage(responseRole, responseStringBuilder.ToString());
            }
        }

        /// <summary>
        /// Truncates the context of the chat messages when the HttpRequestException contains the "context_length_exceeded" code.
        /// </summary>
        /// <param name="ex">The HttpRequestException that was thrown.</param>
        /// <returns>True if the context was truncated, false otherwise.</returns>
        private bool TruncateContextWhenExceeded(HttpRequestException ex)
        {
            if (!ex.Data.Contains("code") || string.IsNullOrWhiteSpace(ex.Data["code"]?.ToString()) || !ex.Data["code"].Equals("context_length_exceeded"))
            {
                throw ex;
            }

            for (int i = 0; i < Messages.Count; i++)
            {
                if (Messages[i].Role != ChatMessageRole.System)
                {
                    Messages.RemoveAt(i);

                    return true;
                }
            }

            return false;
        }

        #endregion Methods
    }
}