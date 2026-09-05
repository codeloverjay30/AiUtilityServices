namespace AiUtility.GeminiUtilityServices.Configs
{
    /// <summary>
    /// Represents configuration settings used to access the Gemini API.
    /// </summary>
    public sealed class GeminiApiOptions
    {
        /// <summary>
        /// Gets or initializes the Gemini model identifier.
        /// </summary>
        public required string Model { get; init; }

        /// <summary>
        /// Gets or initializes the Gemini API version.
        /// </summary>
        public string ApiVersion { get; init; } = "v1beta";

        /// <summary>
        /// Gets or initializes the Gemini API base address.
        /// </summary>
        public Uri BaseAddress { get; init; } =
            new("https://generativelanguage.googleapis.com/");
    }
}