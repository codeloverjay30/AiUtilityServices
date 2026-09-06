using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models;

/// <summary>
/// Provides source-generated JSON metadata for Gemini API models.
/// </summary>
[JsonSerializable(typeof(GeminiGenerateRequest))]
[JsonSerializable(typeof(GeminiResponse))]
[JsonSerializable(typeof(GeminiPart))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public partial class GeminiJsonContext : JsonSerializerContext
{
}