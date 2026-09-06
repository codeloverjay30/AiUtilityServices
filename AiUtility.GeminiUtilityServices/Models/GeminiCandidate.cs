using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    /// <summary>
    /// Represents a candidate response returned by the Gemini API.
    /// </summary>
    public sealed class GeminiCandidate
    {
        /// <summary>
        /// Gets or sets the generated candidate content.
        /// </summary>
        [JsonPropertyName("content")]
        public GeminiMessage Content { get; set; } = new();

        /// <summary>
        /// Gets or sets the reason why Gemini stopped generating the candidate.
        /// </summary>
        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }

        /// <summary>
        /// Gets or sets the candidate index returned by Gemini.
        /// </summary>
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        /// <summary>
        /// Gets or sets the average log probability of the candidate,
        /// when the API provides it.
        /// </summary>
        [JsonPropertyName("avgLogprobs")]
        public double? AverageLogProbabilities { get; set; }

        /// <summary>
        /// Creates a deep copy of the current candidate.
        /// </summary>
        /// <returns>
        /// A new <see cref="GeminiCandidate"/> containing copied values.
        /// </returns>
        public GeminiCandidate DeepClone()
        {
            return new GeminiCandidate
            {
                Content = Content.DeepClone(),
                FinishReason = FinishReason,
                Index = Index,
                AverageLogProbabilities = AverageLogProbabilities
            };
        }

        /// <summary>
        /// Creates a nullable deep copy of the current candidate.
        /// </summary>
        /// <returns>
        /// A copied candidate instance.
        /// </returns>
        public GeminiCandidate? NullableDeepClone()
        {
            return DeepClone();
        }
    }
}