using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models;

/// <summary>
/// Represents a safety rating returned by the Gemini API.
/// </summary>
public sealed class GeminiSafetyRating
{
    /// <summary>
    /// Gets or sets the harm category evaluated by Gemini.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the probability that the content belongs to the harm category.
    /// </summary>
    [JsonPropertyName("probability")]
    public string? Probability { get; set; }
}