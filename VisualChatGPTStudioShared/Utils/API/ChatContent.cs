using Newtonsoft.Json;
using System;

namespace JeffPires.VisualChatGPTStudio.Utils.API
{
    /// <summary>
    /// Represents the base class for chat content, providing a common type identifier.
    /// </summary>
    /// <param name="type">The type of the chat content.</param>
    public abstract class ChatContentBase(string type)
    {
        /// <summary>
        /// Represents the type of the object, initialized with a default value.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; private set; } = type;
    }

    /// <summary>
    /// Represents the content of a chat message in text format.
    /// Inherits from the ChatContentBase class, specifying the content type as "text".
    /// </summary>
    /// <param name="text">The text content of the chat message.</param>
    public class ChatContentForText(string text) : ChatContentBase("text")
    {
        /// <summary>
        /// Represents a text property that is serialized to JSON with the key "text".
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; } = text;
    }

    /// <summary>
    /// Represents the content of a chat message that contains an image, 
    /// initialized with the specified image data.
    /// </summary>
    /// <param name="imageData">The byte array representing the image data.</param>
    public class ChatContentForImage(byte[] imageData) : ChatContentBase("image_url")
    {
        /// <summary>
        /// Represents the URL of an image, initialized with a base64 encoded JPEG image data.
        /// </summary>
        [JsonProperty("image_url")]
        public ImageUrl ImageUrl { get; private set; } = new ImageUrl("data:image/jpeg;base64," + Convert.ToBase64String(imageData));
    }

    /// <summary>
    /// Represents an image URL with a specified URL string.
    /// </summary>
    /// <param name="url">The URL of the image.</param>
    public class ImageUrl(string url)
    {
        /// <summary>
        /// Represents a URL property that can be serialized to and from JSON.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; } = url;
    }
}