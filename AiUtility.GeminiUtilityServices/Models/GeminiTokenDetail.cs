using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models;

/// <summary>
/// Represents token usage details for a specific Gemini content modality.
/// </summary>
public sealed class GeminiTokenDetail
{
    /// <summary>
    /// Gets or sets the content modality associated with the token count.
    /// </summary>
    [JsonPropertyName("modality")]
    public string? Modality { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens consumed by the modality.
    /// </summary>
    [JsonPropertyName("tokenCount")]
    public int TokenCount { get; set; }

    /// <summary>
    /// Creates a deep copy of the current token detail.
    /// </summary>
    /// <returns>
    /// A new <see cref="GeminiTokenDetail"/> containing copied values.
    /// </returns>
    public GeminiTokenDetail DeepClone()
    {
        return new GeminiTokenDetail
        {
            Modality = Modality,
            TokenCount = TokenCount
        };
    }
}