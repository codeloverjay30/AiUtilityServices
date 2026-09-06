using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.TransportModels;

/// <summary>
/// Represents the wire contract for Gemini token usage details.
/// </summary>
internal sealed class GeminiTokenDetailDto
{
    /// <summary>
    /// Gets or initializes the Gemini content modality.
    /// </summary>
    [JsonPropertyName("modality")]
    public string? Modality { get; init; }

    /// <summary>
    /// Gets or initializes the token count reported by Gemini.
    /// </summary>
    [JsonPropertyName("tokenCount")]
    public int TokenCount { get; init; }
}