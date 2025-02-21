using Newtonsoft.Json;
using System.Collections.Generic;

namespace JeffPires.VisualChatGPTStudio.Utils.API
{
    /// <summary>
    /// Class to request functions executions from AI
    /// </summary>
    public class FunctionRequest
    {
        /// <summary>
        /// Request type
        /// </summary>
        [JsonProperty("type")]
        public string Type => "function";

        /// <summary>
        /// Details of the function to be executed
        /// </summary>
        [JsonProperty("function")]
        public Function Function { get; set; }
    }

    /// <summary>
    /// Details of the function to be executed
    /// </summary>
    public class Function
    {
        /// <summary>
        /// Function name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Details on when and how to use the function
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Function parameters
        /// </summary>
        [JsonProperty("parameters")]
        public Parameter Parameters { get; set; }

        /// <summary>
        /// Setting strict to true will ensure function calls reliably adhere to the function schema, instead of being best effort. We recommend always enabling strict mode.
        /// </summary>
        [JsonProperty("strict")]
        public bool Strict => true;
    }

    /// <summary>
    /// Function parameter
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// Parameter type
        /// </summary>
        [JsonProperty("type")]
        public string Type => "object";

        /// <summary>
        /// Parameter properties list
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<string, Property> Properties { get; set; }

        /// <summary>
        /// Indicate if the parameter is mandatory
        /// </summary>
        [JsonProperty("required")]
        public List<string> Required { get; set; }

        /// <summary>
        /// Must be set to false for each object in the parameters
        /// </summary>
        [JsonProperty("additionalProperties")]
        public bool AdditionalProperties => false;
    }

    /// <summary>
    /// Represents a parameter property
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Property type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Property description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
