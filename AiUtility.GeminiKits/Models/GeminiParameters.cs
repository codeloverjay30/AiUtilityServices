using AiUtility.ToolKits.Models;
using System.Text.Json.Serialization;
using AiUtility.GeminiKits.Converters;

namespace AiUtility.GeminiKits.Models
{
    /// <summary>
    /// Represents the Gemini JSON schema for function parameters.
    /// </summary>
    [JsonConverter(typeof(GeminiParametersJsonConverter))]
    public class GeminiParameters
        : AiParametersBase
    {
        /// <summary>
        /// Gets or sets the root schema type.
        /// </summary>
        [JsonPropertyName("type")]
        public new string Type
        {
            get => base.Type;
            set => base.Type = value;
        }

        /// <summary>
        /// Gets or sets the parameter property definitions.
        /// </summary>
        [JsonPropertyName("properties")]
        public new Dictionary<string, GeminiParameterProperty> Properties
        {
            get => base.Properties.ToDictionary(
                static item => item.Key,
                static item => (GeminiParameterProperty)item.Value);

            set => base.Properties =
                value.ToDictionary(
                    static item => item.Key,
                    static item =>
                        (AiParameterPropertyBase)item.Value);
        }

        /// <summary>
        /// Gets or sets the required parameter names.
        /// </summary>
        [JsonPropertyName("required")]
        public new List<string> Required
        {
            get => base.Required;
            set => base.Required = value;
        }
    }
}