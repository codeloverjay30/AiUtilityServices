using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models;

/// <summary>
/// Represents lifecycle status information for a Gemini model.
/// </summary>
public sealed class GeminiModelStatus
{
    /// <summary>
    /// Gets or sets the lifecycle stage of the underlying Gemini model.
    /// </summary>
    [JsonPropertyName("modelStage")]
    public string? ModelStage { get; set; }

    /// <summary>
    /// Gets or sets the scheduled retirement time of the Gemini model.
    /// </summary>
    [JsonPropertyName("retirementTime")]
    public DateTimeOffset? RetirementTime { get; set; }

    /// <summary>
    /// Gets or sets the message describing the current model status.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}