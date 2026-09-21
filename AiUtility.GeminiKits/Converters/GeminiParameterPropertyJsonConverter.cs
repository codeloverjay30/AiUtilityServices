using System.Text.Json;
using System.Text.Json.Serialization;
using AiUtility.GeminiKits.Models;

namespace AiUtility.GeminiKits.Converters;

/// <summary>
/// Serializes Gemini parameter properties using the Gemini wire contract.
/// </summary>
public sealed class GeminiParameterPropertyJsonConverter
    : JsonConverter<GeminiParameterProperty>
{
    /// <summary>
    /// Reads a Gemini parameter property from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized parameter property.</returns>
    public override GeminiParameterProperty Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                "Gemini parameter property must be a JSON object.");
        }

        GeminiParameterProperty result = new();

        if (root.TryGetProperty("type", out JsonElement type))
        {
            if (type.ValueKind != JsonValueKind.String)
            {
                throw new JsonException(
                    "Gemini parameter property 'type' must be a string.");
            }

            result.Type = type.GetString()!;
        }

        if (root.TryGetProperty("description", out JsonElement description))
        {
            if (description.ValueKind != JsonValueKind.String)
            {
                throw new JsonException(
                    "Gemini parameter property 'description' must be a string.");
            }

            result.Description = description.GetString()!;
        }

        if (root.TryGetProperty("enum", out JsonElement enumValues))
        {
            if (enumValues.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException(
                    "Gemini parameter property 'enum' must be an array.");
            }

            result.Enum =
                enumValues.Deserialize<List<string>>(options)
                ?? throw new JsonException(
                    "Gemini parameter property 'enum' cannot be null.");
        }

        if (root.TryGetProperty("properties", out JsonElement properties))
        {
            if (properties.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException(
                    "Gemini parameter property 'properties' must be an object.");
            }

            result.Properties =
                properties.Deserialize<
                    Dictionary<string, GeminiParameterProperty>>(options)
                ?? throw new JsonException(
                    "Gemini parameter property 'properties' cannot be null.");
        }

        if (root.TryGetProperty("required", out JsonElement required))
        {
            if (required.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException(
                    "Gemini parameter property 'required' must be an array.");
            }

            result.Required =
                required.Deserialize<List<string>>(options)
                ?? throw new JsonException(
                    "Gemini parameter property 'required' cannot be null.");
        }

        if (root.TryGetProperty("items", out JsonElement items))
        {
            if (items.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException(
                    "Gemini parameter property 'items' must be an object.");
            }

            result.Items =
                items.Deserialize<GeminiParameterProperty>(options)
                ?? throw new JsonException(
                    "Gemini parameter property 'items' cannot be null.");
        }

        return result;
    }

    /// <summary>
    /// Writes a Gemini parameter property using lowercase wire field names.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The parameter property to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(
        Utf8JsonWriter writer,
        GeminiParameterProperty value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        writer.WriteString("description", value.Description);

        if (value.Enum is not null)
        {
            writer.WritePropertyName("enum");
            JsonSerializer.Serialize(writer, value.Enum, options);
        }

        if (value.Properties is not null)
        {
            writer.WritePropertyName("properties");
            JsonSerializer.Serialize(writer, value.Properties, options);
        }

        if (value.Required is not null)
        {
            writer.WritePropertyName("required");
            JsonSerializer.Serialize(writer, value.Required, options);
        }

        if (value.Items is not null)
        {
            writer.WritePropertyName("items");
            JsonSerializer.Serialize(writer, value.Items, options);
        }

        writer.WriteEndObject();
    }
}