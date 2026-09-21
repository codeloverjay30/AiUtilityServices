using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    /// <summary>
    /// Represents a structured response returned by an executed Gemini function.
    /// </summary>
    public class GeminiFunctionResponse
    {
        /// <summary>
        /// Gets or sets the name of the executed function.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// Gets or sets the function name as read-only character memory.
        /// </summary>
        [JsonIgnore]
        public ReadOnlyMemory<char> RawName
        {
            get
            {
                return Name.AsMemory();
            }

            set
            {
                Name = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the structured response returned by the executed function.
        /// </summary>
        [JsonPropertyName("response")]
        public JsonElement Response
        {
            get;
            set;
        } = CreateEmptyResponse();

        /// <summary>
        /// Gets or sets the structured response as raw JSON text.
        /// </summary>
        /// <remarks>
        /// The supplied value must represent a JSON object because the Gemini
        /// function-response wire contract requires a structured response.
        /// </remarks>
        [JsonIgnore]
        public ReadOnlyMemory<char> RawResponse
        {
            get
            {
                return Response.GetRawText()
                    .AsMemory();
            }

            set
            {
                Response =
                    ParseResponse(
                        value);
            }
        }

        /// <summary>
        /// Creates an independent clone of this function response.
        /// </summary>
        /// <returns>
        /// A cloned function response.
        /// </returns>
        public GeminiFunctionResponse DeepClone()
        {
            return new GeminiFunctionResponse
            {
                Name = Name,
                Response = Response.Clone(),
            };
        }

        /// <summary>
        /// Creates an independent nullable clone of this function response.
        /// </summary>
        /// <returns>
        /// A cloned function response.
        /// </returns>
        public GeminiFunctionResponse? NullableDeepClone()
        {
            return DeepClone();
        }

        /// <summary>
        /// Parses raw JSON into a structured Gemini function response.
        /// </summary>
        /// <param name="rawResponse">
        /// The raw JSON response.
        /// </param>
        /// <returns>
        /// The parsed JSON object.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the response is empty, invalid JSON, or is not a JSON object.
        /// </exception>
        private static JsonElement ParseResponse(
            ReadOnlyMemory<char> rawResponse)
        {
            if (rawResponse.IsEmpty)
            {
                throw new ArgumentException(
                    "Gemini function response JSON cannot be empty.",
                    nameof(rawResponse));
            }

            try
            {
                using var document =
                    JsonDocument.Parse(
                        rawResponse);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException(
                        "Gemini function response JSON must contain a JSON object.",
                        nameof(rawResponse));
                }

                return document.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                throw new ArgumentException(
                    "Gemini function response contains invalid JSON.",
                    nameof(rawResponse),
                    ex);
            }
        }

        /// <summary>
        /// Creates the default structured Gemini function response.
        /// </summary>
        /// <returns>
        /// An empty JSON object.
        /// </returns>
        private static JsonElement CreateEmptyResponse()
        {
            return JsonSerializer.SerializeToElement(
                new
                {
                });
        }
    }
}