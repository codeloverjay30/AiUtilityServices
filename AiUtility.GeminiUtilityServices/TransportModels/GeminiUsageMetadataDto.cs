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
    /// Gets or initializes the generated candidate token count.
    /// </summary>
    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes generated candidate token details.
    /// </summary>
    [JsonPropertyName("candidatesTokensDetails")]
    public List<GeminiTokenDetailDto> CandidatesTokensDetails { get; init; } = [];

    /// <summary>
    /// Gets or initializes the cached-content token count.
    /// </summary>
    [JsonPropertyName("cachedContentTokenCount")]
    public int CachedContentTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes cached-content token details.
    /// </summary>
    [JsonPropertyName("cacheTokensDetails")]
    public List<GeminiTokenDetailDto> CacheTokensDetails { get; init; } = [];

    /// <summary>
    /// Gets or initializes the token count used for tool-use prompts.
    /// </summary>
    [JsonPropertyName("toolUsePromptTokenCount")]
    public int ToolUsePromptTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes token details used for tool-use prompts.
    /// </summary>
    [JsonPropertyName("toolUsePromptTokensDetails")]
    public List<GeminiTokenDetailDto> ToolUsePromptTokensDetails { get; init; } = [];

    /// <summary>
    /// Gets or initializes the number of tokens consumed by model thinking.
    /// </summary>
    [JsonPropertyName("thoughtsTokenCount")]
    public int ThoughtsTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes the total token count.
    /// </summary>
    [JsonPropertyName("totalTokenCount")]
    public int TotalTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes the service tier used to process the request.
    /// </summary>
    [JsonPropertyName("serviceTier")]
    public string? ServiceTier { get; init; }
}