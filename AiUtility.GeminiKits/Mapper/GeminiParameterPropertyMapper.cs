using AiUtility.GeminiKits.Models;
using AiUtility.ToolKits.Models;

namespace AiUtility.GeminiKits.Mappers;

/// <summary>
/// Maps provider-independent parameter schemas to Gemini parameter schemas.
/// </summary>
public sealed class GeminiParameterPropertyMapper
    : IGeminiParameterPropertyMapper
{
    /// <summary>
    /// Converts a provider-independent parameter schema to a Gemini schema.
    /// </summary>
    /// <param name="source">
    /// The source parameter schema.
    /// </param>
    /// <returns>
    /// The converted Gemini parameter schema.
    /// </returns>
    public GeminiParameterProperty Map(
        AiParameterPropertyBase source)
    {
        ArgumentNullException.ThrowIfNull(
            source);

        var result =
            new GeminiParameterProperty
            {
                Type =
                    source.Type,

                Description =
                    source.Description,

                Enum =
                    source.Enum is null
                        ? null
                        : new List<string>(
                            source.Enum),

                Required =
                    source.Required is null
                        ? null
                        : new List<string>(
                            source.Required)
            };

        if (source.Properties is not null)
        {
            result.Properties =
                source.Properties.ToDictionary(
                    static item =>
                        item.Key,

                    item =>
                            Map(
                                item.Value));
        }

        if (source.Items is not null)
        {
            result.Items =
                Map(
                    source.Items);
        }

        return result;
    }
}