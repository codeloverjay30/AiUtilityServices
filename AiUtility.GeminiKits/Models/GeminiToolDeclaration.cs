using AiUtility.ToolKits.Models;
using System.Text.Json.Serialization;
using AiUtility.GeminiKits.Converters;

namespace AiUtility.GeminiKits.Models
{
    /// <summary>
    /// Represents a Gemini function declaration.
    /// </summary>
    [JsonConverter(typeof(GeminiToolDeclarationJsonConverter))]
    public class GeminiToolDeclaration
        : AiToolDeclarationBase
    {
        /// <summary>
        /// Gets or sets the unique function name exposed to Gemini.
        /// </summary>
        [JsonPropertyName("name")]
        public new string Name
        {
            get => base.Name;
            set => base.Name = value;
        }

        /// <summary>
        /// Gets or sets the description of the function exposed to Gemini.
        /// </summary>
        [JsonPropertyName("description")]
        public new string Description
        {
            get => base.Description;
            set => base.Description = value;
        }

        /// <summary>
        /// Gets or sets the function parameter schema exposed to Gemini.
        /// </summary>
        [JsonPropertyName("parameters")]
        public new GeminiParameters Parameters
        {
            get => (GeminiParameters)base.Parameters;
            set => base.Parameters = value;
        }
    }
}