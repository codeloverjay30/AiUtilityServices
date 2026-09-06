namespace AiUtility.GeminiUtilityServices.Models;

/// <summary>
/// Represents token usage for a logical Gemini usage category.
/// </summary>
public sealed class GeminiTokenUsage
{
    /// <summary>
    /// Gets or initializes the total number of tokens consumed by the category.
    /// </summary>
    public int TokenCount { get; init; }

    /// <summary>
    /// Gets or initializes token usage details grouped by modality.
    /// </summary>
    public IReadOnlyList<GeminiTokenDetail> Details { get; init; }
        = Array.Empty<GeminiTokenDetail>();

    /// <summary>
    /// Creates a deep copy of the current token usage.
    /// </summary>
    /// <returns>
    /// A new token usage instance containing independent detail objects.
    /// </returns>
    public GeminiTokenUsage DeepClone()
    {
        return new GeminiTokenUsage
        {
            TokenCount = TokenCount,
            Details = Details
                .Select(static detail => detail.DeepClone())
                .ToArray()
        };
    }
}