using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models;

/// <summary>
/// Represents feedback returned by Gemini for the submitted prompt.
/// </summary>
public sealed class GeminiPromptFeedback
{
    /// <summary>
    /// Gets or sets the reason why the submitted prompt was blocked.
/// </summary>
    [JsonPropertyName("blockReason")]
    public string? BlockReason { get; set; }

    /// <summary>
    /// Gets or sets safety ratings associated with the submitted prompt.
    /// </summary>
    [JsonPropertyName("safetyRatings")]
    public List<GeminiSafetyRating> SafetyRatings { get; set; } = [];
}
