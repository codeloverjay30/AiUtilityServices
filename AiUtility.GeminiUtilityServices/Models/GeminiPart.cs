using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    /// <summary>
    /// Represents a content part exchanged with the Gemini API.
    /// </summary>
    public class GeminiPart
    {
        /// <summary>
        /// Gets or sets the textual content of the part.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets a zero-copy view over the textual content.
        /// </summary>
        [JsonIgnore]
        public ReadOnlyMemory<char> RawText
        {
            get => string.IsNullOrEmpty(Text)
                ? ReadOnlyMemory<char>.Empty
                : Text.AsMemory();

            set => Text = value.Span.ToString();
        }

        /// <summary>
        /// Gets or sets inline binary data associated with the part.
        /// </summary>
        [JsonPropertyName("inline_data")]
        public GeminiInlineData? InlineData { get; set; }

        /// <summary>
        /// Gets or sets a function call emitted by the model.
        /// </summary>
        [JsonPropertyName("function_call")]
        public GeminiFunctionCall? FunctionCall { get; set; }

        /// <summary>
        /// Gets or sets a function response associated with this part.
        /// </summary>
        [JsonPropertyName("function_response")]
        public GeminiFunctionResponse? FunctionResponse { get; set; }

        /// <summary>
        /// Gets or sets the opaque thought signature returned by Gemini.
        /// The value must be preserved when the part is reused in a
        /// subsequent conversation turn.
        /// </summary>
        [JsonPropertyName("thoughtSignature")]
        public string? ThoughtSignature { get; set; }

        /// <summary>
        /// Creates a deep copy of the current Gemini part.
        /// </summary>
        /// <returns>
        /// A new <see cref="GeminiPart"/> containing copied values.
        /// </returns>
        public GeminiPart DeepClone()
        {
            return new GeminiPart
            {
                Text = Text,
                InlineData = InlineData?.NullableDeepClone(),
                FunctionCall = FunctionCall?.DeepClone(),
                FunctionResponse = FunctionResponse?.NullableDeepClone(),
                ThoughtSignature = ThoughtSignature
            };
        }
    }
}