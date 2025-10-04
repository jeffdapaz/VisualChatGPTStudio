using Newtonsoft.Json;
using System.Collections.Generic;

namespace VisualChatGPTStudioShared.Agents.CodeEditAgent
{
    /// <summary>
    /// Represents the payload for an edit code function call, typically used to encapsulate the data required for code editing operations in a chat or function-calling context.
    /// </summary>
    public class EditCodeFunctionPayload
    {
        /// <summary>
        /// Gets or sets the list of edit operations associated with this object. 
        /// </summary>
        [JsonProperty("edits")]
        public List<EditOperation> Edits { get; set; }
    }

    /// <summary>
    /// Represents an edit operation, typically used to describe a change or modification within a document or code editor.
    /// </summary>
    public class EditOperation
    {
        /// <summary>
        /// Gets or sets the type of operation to perform in a code diff or edit action.
        /// Valid values are "insert", "replace", or "delete".
        /// </summary>
        [JsonProperty("operation")]
        public string Operation { get; set; }

        /// <summary>
        /// Gets or sets the starting position for a range or selection.
        /// </summary>
        [JsonProperty("start")]
        public Position Start { get; set; }

        /// <summary>
        /// Gets or sets the end position for an operation, such as an insert. This property is optional for insert.
        /// </summary>
        [JsonProperty("end")]
        public Position End { get; set; }

        /// <summary>
        /// Gets or sets the content (code). Required for insert and replace
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    /// <summary>
    /// Represents a position or coordinate, typically used to define a point or location.
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Gets or sets the line number (1-based) associated with this object.
        /// </summary>
        [JsonProperty("line")]
        public int Line { get; set; }

        /// <summary>
        /// Gets or sets the column number (1-based) for this item.
        /// </summary>
        [JsonProperty("column")]
        public int Column { get; set; } // 1-based
    }

}