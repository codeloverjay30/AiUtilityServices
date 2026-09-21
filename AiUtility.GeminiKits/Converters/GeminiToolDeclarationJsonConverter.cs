using System.Text.Json;
using System.Text.Json.Serialization;
using AiUtility.GeminiKits.Models;

namespace AiUtility.GeminiKits.Converters;

/// <summary>
/// Serializes Gemini tool declarations using the Gemini wire contract.
/// </summary>
public sealed class GeminiToolDeclarationJsonConverter
    : JsonConverter<GeminiToolDeclaration>
{
    /// <summary>
    /// Reads a Gemini tool declaration from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized Gemini tool declaration.</returns>
    public override GeminiToolDeclaration Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                "Gemini tool declaration must be a JSON object.");
        }

        if (!root.TryGetProperty("name", out JsonElement name) ||
            name.ValueKind != JsonValueKind.String)
        {
            throw new JsonException(
                "Gemini tool declaration requires a string 'name'.");
        }

        if (!root.TryGetProperty(
                "description",
                out JsonElement description) ||
            description.ValueKind != JsonValueKind.String)
        {
            throw new JsonException(
                "Gemini tool declaration requires a string 'description'.");
        }

        if (!root.TryGetProperty(
                "parameters",
                out JsonElement parameters) ||
            parameters.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                "Gemini tool declaration requires an object 'parameters'.");
        }

        GeminiParameters parsedParameters =
            parameters.Deserialize<GeminiParameters>(options)
            ?? throw new JsonException(
                "Gemini tool declaration parameters cannot be null.");

        return new GeminiToolDeclaration
        {
            Name = name.GetString()!,
            Description = description.GetString()!,
            Parameters = parsedParameters
        };
    }

    /// <summary>
    /// Writes a Gemini tool declaration using lowercase wire field names.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The declaration to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(
        Utf8JsonWriter writer,
        GeminiToolDeclaration value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WriteString("description", value.Description);

        writer.WritePropertyName("parameters");
        JsonSerializer.Serialize(writer, value.Parameters, options);

        writer.WriteEndObject();
    }
}