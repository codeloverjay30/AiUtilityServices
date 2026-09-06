namespace AiUtility.GeminiUtilityServices.Models
{
    /// <summary>
    /// Represents a message exchanged with the Gemini API.
    /// </summary>
    public class GeminiMessage
    {
        /// <summary>
        /// Gets or sets the role of the message sender.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("role")]
        public string Role { get; set; } =
            AiUtility.AiBaseUtilityServices.Consts.Constants
                .AiApi.GeminiAiStudio.AiSchema.Roles.USER;

        /// <summary>
        /// Gets or sets the content parts contained in the message.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();

        /// <summary>
        /// Creates a deep copy of the current message.
        /// </summary>
        /// <returns>
        /// A new <see cref="GeminiMessage"/> containing independent copies
        /// of all message parts.
        /// </returns>
        public GeminiMessage DeepClone()
        {
            return new GeminiMessage
            {
                Role = Role,
                Parts = Parts
                    .Select(part => part.DeepClone())
                    .ToList()
            };
        }

        /// <summary>
        /// Creates a nullable deep copy of the current message.
        /// </summary>
        /// <returns>
        /// A deep copy of the current message.
        /// </returns>
        public GeminiMessage? NullableDeepClone()
        {
            return DeepClone();
        }
    }
}