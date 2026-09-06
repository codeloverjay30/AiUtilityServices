using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.TransportModels;

/// <summary>
/// Represents the raw usage metadata contract returned by the Gemini API.
/// </summary>
internal sealed class GeminiUsageMetadataDto
{
    /// <summary>
    /// Gets or initializes the prompt token count.
    /// </summary>
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes prompt token details.
    /// </summary>
    [JsonPropertyName("promptTokensDetails")]
    public List<GeminiTokenDetailDto> PromptTokensDetails { get; init; } = [];

    /// <summary>
    /// Gets or initializes candidate token count.
    /// </summary>
    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes candidate token details.
    /// </summary>
    [JsonPropertyName("candidatesTokensDetails")]
    public List<GeminiTokenDetailDto> CandidatesTokensDetails { get; init; } = [];

    /// <summary>
    /// Gets or initializes cached-content token count.
    /// </summary>
    [JsonPropertyName("cachedContentTokenCount")]
    public int CachedContentTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes cached-content token details.
    /// </summary>
    [JsonPropertyName("cacheTokensDetails")]
    public List<GeminiTokenDetailDto> CacheTokensDetails { get; init; } = [];

    /// <summary>
    /// Gets or initializes thinking token count.
    /// </summary>
    [JsonPropertyName("thoughtsTokenCount")]
    public int ThoughtsTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes the total token count.
    /// </summary>
    [JsonPropertyName("totalTokenCount")]
    public int TotalTokenCount { get; init; }
}