using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.TransportModels;

namespace AiUtility.GeminiUtilityServices.Mappers;

/// <summary>
/// Maps Gemini usage metadata transport models to normalized application models.
/// </summary>
internal static class GeminiUsageMetadataMapper
{
    /// <summary>
    /// Maps raw Gemini usage metadata to the normalized usage model.
    /// </summary>
    /// <param name="source">
    /// The raw Gemini usage metadata.
    /// </param>
    /// <returns>
    /// The normalized usage metadata.
    /// </returns>
    public static GeminiUsageMetadata Map(
        GeminiUsageMetadataDto source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new GeminiUsageMetadata
        {
            Prompt = CreateUsage(
                source.PromptTokenCount,
                source.PromptTokensDetails),

            Candidates = CreateUsage(
                source.CandidatesTokenCount,
                source.CandidatesTokensDetails),

            Cache = CreateUsage(
                source.CachedContentTokenCount,
                source.CacheTokensDetails),

            ThoughtsTokenCount = source.ThoughtsTokenCount,
            TotalTokenCount = source.TotalTokenCount
        };
    }

    /// <summary>
    /// Creates normalized token usage from the specified transport values.
    /// </summary>
    /// <param name="tokenCount">
    /// The aggregate token count.
    /// </param>
    /// <param name="details">
    /// The raw token detail collection.
    /// </param>
    /// <returns>
    /// The normalized token usage.
    /// </returns>
    private static GeminiTokenUsage CreateUsage(
        int tokenCount,
        IReadOnlyCollection<GeminiTokenDetailDto>? details)
    {
        return new GeminiTokenUsage
        {
            TokenCount = tokenCount,
            Details = details is null
                ? Array.Empty<GeminiTokenDetail>()
                : details
                    .Select(static detail =>
                        new GeminiTokenDetail
                        {
                            Modality = detail.Modality ?? string.Empty,
                            TokenCount = detail.TokenCount
                        })
                    .ToArray()
        };
    }
}