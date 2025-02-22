using Newtonsoft.Json;
using System;

namespace OpenAI_API.Chat
{
    /// <summary>
    /// Represents the base class for chat content, providing a common type identifier.
    /// </summary>
    public abstract class ChatContentBase
    {
        /// <summary>
        /// Represents the type of the object, initialized with a default value.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ChatContentBase class with the specified type.
        /// </summary>
        /// <param name="type">The type of the chat content.</param>
        public ChatContentBase(string type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Represents the content of a chat message in text format.
    /// Inherits from the ChatContentBase class, specifying the content type as "text".
    /// </summary>
    public class ChatContentForText : ChatContentBase
    {
        /// <summary>
        /// Represents a text property that is serialized to JSON with the key "text".
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Initializes a new instance of the ChatContentForText class with the specified text.
        /// </summary>
        /// <param name="text">The text content to initialize the instance with.</param>
        public ChatContentForText(string text) : base(text)
        {
            Text = text;
        }
    }

    /// <summary>
    /// Represents the content of a chat message that contains an image, 
    /// initialized with the specified image data.
    /// </summary>
    public class ChatContentForImage : ChatContentBase
    {
        /// <summary>
        /// Represents the URL of an image, initialized with a base64 encoded JPEG image data.
        /// </summary>
        [JsonProperty("image_url")]
        public ImageUrl ImageUrl { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ChatContentForImage class with the provided image data.
        /// Converts the byte array to a Base64 string and constructs an image URL in the "data:image/jpeg;base64" format.
        /// </summary>
        /// <param name="imageData">The byte array representing the image data.</param>
        public ChatContentForImage(byte[] imageData) : base("image_url")
        {
            ImageUrl = new ImageUrl("data:image/jpeg;base64," + Convert.ToBase64String(imageData));
        }
    }

    /// <summary>
    /// Represents an image URL with a specified URL string.
    /// </summary>
    public class ImageUrl
    {
        /// <summary>
        /// Represents a URL property that can be serialized to and from JSON.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Initializes a new instance of the ImageUrl class with the specified URL.
        /// </summary>
        /// <param name="url">The URL of the image.</param>
        public ImageUrl(string url)
        {
            Url = url;
        }
    }
}
