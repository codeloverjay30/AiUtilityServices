using AiUtility.GeminiKits.Models;
using AiUtility.ToolKits.Models;

namespace AiUtility.GeminiKits.Mappers;

/// <summary>
/// Defines a mapper for converting provider-independent parameter schemas
/// to Gemini parameter schemas.
/// </summary>
public interface IGeminiParameterPropertyMapper
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
    GeminiParameterProperty Map(
        AiParameterPropertyBase source);
}