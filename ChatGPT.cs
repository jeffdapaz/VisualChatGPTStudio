﻿using JeffPires.VisualChatGPTStudio.Options;
using OpenAI_API;
using OpenAI_API.Completions;
using OpenAI_API.Models;

namespace JeffPires.VisualChatGPTStudio
{
    /// <summary>
    /// Static class containing methods for interacting with the ChatGPT API.
    /// </summary>
    static class ChatGPT
    {
        /// <summary>
        /// Requests a completion from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request to send to the API.</param>
        /// <param name="resultHandler">The action to take when the result is received.</param>
        /// <returns>A task representing the completion request.</returns>
        public static async Task RequestAsync(OptionPageGridGeneral options, string request, Action<int, CompletionResult> resultHandler)
        {
            OpenAIAPI api = new(options.ApiKey);

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

            CompletionRequest completionRequest = new(request, model, options.MaxTokens, options.Temperature, presencePenalty: options.PresencePenalty, frequencyPenalty: options.FrequencyPenalty, top_p: options.TopP);

            await api.Completions.StreamCompletionAsync(completionRequest, resultHandler);
        }
    }
}
