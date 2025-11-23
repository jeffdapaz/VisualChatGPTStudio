using Newtonsoft.Json;

namespace OpenAI_API.Functions
{
    /// <summary>
    /// Details of the function to be executed
    /// </summary>
    public class FunctionResult
    {
        [JsonProperty("index")]
        public int Index { get; set; }
        
        /// <summary>
        /// Used later to submit the function result back to the AI.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// It will probably always be "function", indicating that the execution of a function is being requested.
        /// </summary>
        /// <returns>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the function to call, represented by the <see cref="FunctionToCall"/> object.
        /// </summary>
        [JsonProperty("function")]
        public FunctionToCall Function { get; set; }
    }

    /// <summary>
    /// Represents a class that encapsulates a function or method to be called.
    /// </summary>
    public class FunctionToCall
    {
        /// <summary>
        /// The name of the function to be executed.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the function's arguments.
        /// </summary>
        [JsonProperty("arguments")]
        public string Arguments { get; set; }
    }
}