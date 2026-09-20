using AiUtility.ToolKits.Models;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiKits.Models
{
    /// <summary>
    /// Represents a Gemini function parameter property.
    /// </summary>
    public class GeminiParameterProperty
        : AiParameterPropertyBase
    {
        /// <summary>
        /// Gets or sets the parameter schema type.
        /// </summary>
        [JsonPropertyName("type")]
        public new string Type
        {
            get => base.Type;
            set => base.Type = value;
        }

        /// <summary>
        /// Gets or sets the parameter description.
        /// </summary>
        [JsonPropertyName("description")]
        public new string Description
        {
            get => base.Description;
            set => base.Description = value;
        }

        /// <summary>
        /// Gets or sets allowed enum values.
        /// </summary>
        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public new List<string>? Enum
        {
            get => base.Enum;
            set => base.Enum = value;
        }

        /// <summary>
        /// Gets or sets nested object property definitions.
        /// </summary>
        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public new Dictionary<string, GeminiParameterProperty>? Properties
        {
            get =>
                base.Properties?.ToDictionary(
                    static item =>
                        item.Key,
                    static item =>
                        (GeminiParameterProperty)item.Value);

            set =>
                base.Properties =
                    value?.ToDictionary(
                        static item =>
                            item.Key,
                        static item =>
                            (AiParameterPropertyBase)item.Value);
        }

        /// <summary>
        /// Gets or sets the schema used for collection items.
        /// </summary>
        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public new GeminiParameterProperty? Items
        {
            get =>
                base.Items as GeminiParameterProperty;

            set =>
                base.Items =
                    value;
        }

        /// <summary>
        /// Gets or sets the required nested property names.
        /// </summary>
        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public new List<string>? Required
        {
            get =>
                base.Required;

            set =>
                base.Required =
                    value;
        }
    }
}