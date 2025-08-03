using Newtonsoft.Json;
using System;

namespace OpenAI_API.ResponsesAPI.Models.Request
{
    public class ComputerUseContent
    {
        [JsonProperty("type")]
        public string Type { get; private set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; private set; }

        [JsonProperty("image_url", NullValueHandling = NullValueHandling.Ignore)]
        public string ImageUrl { get; private set; }

        public ComputerUseContent()
        {

        }

        public ComputerUseContent(string text)
        {
            Type = "input_text";
            Text = text;
        }

        public ComputerUseContent(byte[] imageData)
        {
            Type = "input_image";
            ImageUrl = "data:image/png;base64," + Convert.ToBase64String(imageData);
        }
    }
}