using System.Text.Json;
using System.Text.Json.Serialization;
using AiUtility.GeminiKits.Models;

namespace AiUtility.GeminiKits.Converters;

/// <summary>
/// Serializes Gemini parameter schemas using the Gemini wire contract.
/// </summary>
public sealed class GeminiParametersJsonConverter
    : JsonConverter<GeminiParameters>
{
    /// <summary>
    /// Reads a Gemini parameter schema from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized parameter schema.</returns>
    public override GeminiParameters Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                "Gemini parameters must be a JSON object.");
        }

        GeminiParameters result = new();

        if (root.TryGetProperty("type", out JsonElement type))
        {
            if (type.ValueKind != JsonValueKind.String)
            {
                throw new JsonException(
                    "Gemini parameters 'type' must be a string.");
            }

            result.Type = type.GetString()!;
        }

        if (root.TryGetProperty(
                "properties",
                out JsonElement properties))
        {
            if (properties.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException(
                    "Gemini parameters 'properties' must be an object.");
            }

            result.Properties =
                properties.Deserialize<
                    Dictionary<string, GeminiParameterProperty>>(options)
                ?? throw new JsonException(
                    "Gemini parameters 'properties' cannot be null.");
        }

        if (root.TryGetProperty("required", out JsonElement required))
        {
            if (required.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException(
                    "Gemini parameters 'required' must be an array.");
            }

            result.Required =
                required.Deserialize<List<string>>(options)
                ?? throw new JsonException(
                    "Gemini parameters 'required' cannot be null.");
        }

        return result;
    }

    /// <summary>
    /// Writes a Gemini parameter schema using lowercase wire field names.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The schema to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(
        Utf8JsonWriter writer,
        GeminiParameters value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteString("type", value.Type);

        writer.WritePropertyName("properties");
        JsonSerializer.Serialize(writer, value.Properties, options);

        writer.WritePropertyName("required");
        JsonSerializer.Serialize(writer, value.Required, options);

        writer.WriteEndObject();
    }
}