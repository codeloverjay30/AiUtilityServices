using AiUtility.GeminiUtilityServices.Mappers;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.TransportModels;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Converters;

/// <summary>
/// Converts Gemini usage metadata between the wire contract and normalized model.
/// </summary>
public sealed class GeminiUsageMetadataJsonConverter
    : JsonConverter<GeminiUsageMetadata>
{
    /// <summary>
    /// Reads Gemini usage metadata from its wire-level JSON representation.
    /// </summary>
    /// <param name="reader">
    /// The JSON reader.
    /// </param>
    /// <param name="typeToConvert">
    /// The destination model type.
    /// </param>
    /// <param name="options">
    /// The JSON serialization options.
    /// </param>
    /// <returns>
    /// The normalized Gemini usage metadata.
    /// </returns>
    public override GeminiUsageMetadata Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var dto =
            JsonSerializer.Deserialize<GeminiUsageMetadataDto>(
                ref reader,
                options);

        if (dto is null)
        {
            throw new JsonException(
                "The Gemini usage metadata response was null.");
        }

        return GeminiUsageMetadataMapper.Map(dto);
    }

    /// <summary>
    /// Writes normalized Gemini usage metadata using the Gemini wire contract.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    /// <param name="value">
    /// The normalized usage metadata.
    /// </param>
    /// <param name="options">
    /// The JSON serialization options.
    /// </param>
    public override void Write(
        Utf8JsonWriter writer,
        GeminiUsageMetadata value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        var dto = new GeminiUsageMetadataDto
        {
            PromptTokenCount = value.Prompt.TokenCount,
            PromptTokensDetails = MapDetails(value.Prompt.Details),

            CandidatesTokenCount = value.Candidates.TokenCount,
            CandidatesTokensDetails = MapDetails(value.Candidates.Details),

            CachedContentTokenCount = value.Cache.TokenCount,
            CacheTokensDetails = MapDetails(value.Cache.Details),

            ThoughtsTokenCount = value.ThoughtsTokenCount,
            TotalTokenCount = value.TotalTokenCount
        };

        JsonSerializer.Serialize(
            writer,
            dto,
            options);
    }

    /// <summary>
    /// Maps normalized token details to Gemini transport models.
    /// </summary>
    /// <param name="details">
    /// The normalized token details.
    /// </param>
    /// <returns>
    /// The Gemini wire-level token details.
    /// </returns>
    private static List<GeminiTokenDetailDto> MapDetails(
        IReadOnlyList<GeminiTokenDetail> details)
    {
        ArgumentNullException.ThrowIfNull(details);

        return details
            .Select(static detail =>
                new GeminiTokenDetailDto
                {
                    Modality = detail.Modality,
                    TokenCount = detail.TokenCount
                })
            .ToList();
    }
}