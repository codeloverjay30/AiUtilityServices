using AiUtility.GeminiUtilityServices.Converters;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models;

/// <summary>
/// Represents normalized token usage metadata for a Gemini request.
/// </summary>
[JsonConverter(typeof(GeminiUsageMetadataJsonConverter))]
public sealed class GeminiUsageMetadata
{
    /// <summary>
    /// Gets or initializes token usage associated with the prompt.
    /// </summary>
    public GeminiTokenUsage Prompt { get; init; } = new();

    /// <summary>
    /// Gets or initializes token usage associated with generated candidates.
    /// </summary>
    public GeminiTokenUsage Candidates { get; init; } = new();

    /// <summary>
    /// Gets or initializes token usage associated with cached content.
    /// </summary>
    public GeminiTokenUsage Cache { get; init; } = new();

    /// <summary>
    /// Gets or initializes token usage associated with tool-use prompts.
    /// </summary>
    public GeminiTokenUsage ToolUsePrompt { get; init; } = new();

    /// <summary>
    /// Gets or initializes the number of tokens consumed by model thinking.
    /// </summary>
    public int ThoughtsTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes the total number of tokens consumed by the request.
    /// </summary>
    public int TotalTokenCount { get; init; }

    /// <summary>
    /// Gets or initializes the service tier used to process the request.
    /// </summary>
    public string? ServiceTier { get; init; }

    /// <summary>
    /// Creates a deep copy of the current usage metadata.
    /// </summary>
    /// <returns>
    /// A new usage metadata instance containing independent child models.
    /// </returns>
    public GeminiUsageMetadata DeepCopy()
    {
        return new GeminiUsageMetadata
        {
            Prompt = Prompt.DeepClone(),
            Candidates = Candidates.DeepClone(),
            Cache = Cache.DeepClone(),
            ToolUsePrompt = ToolUsePrompt.DeepClone(),
            ThoughtsTokenCount = ThoughtsTokenCount,
            TotalTokenCount = TotalTokenCount,
            ServiceTier = ServiceTier
        };
    }
}