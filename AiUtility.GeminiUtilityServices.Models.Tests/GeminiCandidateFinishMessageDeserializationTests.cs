using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models.Tests;

/// <summary>
/// Contains regression tests for the Gemini candidate finish-message
/// wire contract.
/// </summary>
public sealed class GeminiCandidateFinishMessageDeserializationTests
{
    /// <summary>
    /// Verifies that the existing candidate wire contract can still be
    /// deserialized when the finish-message property is not present.
    /// </summary>
    [Fact]
    public void Deserialize_WithoutFinishMessage_ShouldPreserveExistingWireContract()
    {
        // Arrange
        const string json =
            """
            {
              "content": {
                "role": "model",
                "parts": [
                  {
                    "text": "Response from AI"
                  }
                ]
              },
              "finishReason": "STOP",
              "index": 0
            }
            """;

        var options =
            CreateStrictJsonOptions();

        // Act
        var result =
            JsonSerializer.Deserialize<GeminiCandidate>(
                json,
                options);

        // Assert
        result.Should()
            .NotBeNull();

        result!.Content.Should()
            .NotBeNull();

        result.Content!.Role.Should()
            .Be(
                "model");

        result.Content.Parts.Should()
            .ContainSingle();

        result.Content.Parts[0]
            .Text.Should()
            .Be(
                "Response from AI");

        result.FinishReason.Should()
            .Be(
                "STOP");

        result.Index.Should()
            .Be(
                0);
    }

    /// <summary>
    /// Verifies that an unrelated unknown candidate wire property remains
    /// rejected by strict deserialization.
    /// </summary>
    [Fact]
    public void Deserialize_UnknownWireProperty_ShouldThrowJsonException()
    {
        // Arrange
        const string json =
            """
            {
              "content": {
                "role": "model",
                "parts": []
              },
              "unexpectedCandidateProperty": "unexpected"
            }
            """;

        var options =
            CreateStrictJsonOptions();

        // Act
        Action act =
            () =>
                JsonSerializer.Deserialize<GeminiCandidate>(
                    json,
                    options);

        // Assert
        act.Should()
            .Throw<JsonException>()
            .WithMessage(
                "*unexpectedCandidateProperty*");
    }

    /// <summary>
    /// Verifies that the Gemini wire property "finishMessage" is mapped to the
    /// candidate finish message.
    /// </summary>
    [Fact]
    public void Deserialize_FinishMessageWireProperty_ShouldPopulateFinishMessage()
    {
        // Arrange
        const string json =
            """
        {
          "finishMessage": "The model stopped generating content."
        }
        """;

        var options =
            CreateStrictJsonOptions();

        // Act
        var result =
            JsonSerializer.Deserialize<GeminiCandidate>(
                json,
                options);

        // Assert
        result.Should()
            .NotBeNull();

        result!.FinishMessage.Should()
            .Be(
                "The model stopped generating content.");
    }

    /// <summary>
    /// Creates JSON serializer options that reject unknown Gemini wire
    /// properties.
    /// </summary>
    /// <returns>
    /// Strict serializer options for Gemini candidate wire-contract tests.
    /// </returns>
    private static JsonSerializerOptions CreateStrictJsonOptions()
    {
        return new JsonSerializerOptions
        {
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
        };
    }
}