using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models.Tests;

/// <summary>
/// Contains regression tests for the Gemini function-call identifier
/// wire contract.
/// </summary>
public sealed class GeminiFunctionCallIdDeserializationTests
{
    /// <summary>
    /// Verifies that the Gemini wire property "id" is mapped to the function
    /// call identifier.
    /// </summary>
    [Fact]
    public void Deserialize_IdWireProperty_ShouldPopulateId()
    {
        // Arrange
        const string json =
            """
        {
          "id": "function-call-001",
          "name": "Click",
          "args": {}
        }
        """;

        var options =
            CreateStrictJsonOptions();

        // Act
        var result =
            JsonSerializer.Deserialize<GeminiFunctionCall>(
                json,
                options);

        // Assert
        result.Should()
            .NotBeNull();

        result!.Id.Should()
            .Be(
                "function-call-001");

        result.Name.Should()
            .Be(
                "Click");

        result.Args.Should()
            .BeEmpty();
    }


    /// <summary>
    /// Verifies that the existing Gemini function-call wire contract can be
    /// deserialized when the identifier is not present.
    /// </summary>
    [Fact]
    public void Deserialize_WithoutId_ShouldPreserveExistingWireContract()
    {
        // Arrange
        const string json =
            """
            {
              "name": "Wait",
              "args": {
                "milliseconds": 1500
              }
            }
            """;

        var options =
            CreateStrictJsonOptions();

        // Act
        var result =
            JsonSerializer.Deserialize<GeminiFunctionCall>(
                json,
                options);

        // Assert
        result.Should()
            .NotBeNull();

        result!.Name.Should()
            .Be(
                "Wait");

        result.Args.Should()
            .ContainKey(
                "milliseconds");

        result.Args[
                "milliseconds"]
            .GetInt32()
            .Should()
            .Be(
                1500);
    }

    /// <summary>
    /// Creates JSON serializer options that reject unknown Gemini wire
    /// properties.
    /// </summary>
    /// <returns>
    /// Strict serializer options for Gemini wire-contract regression tests.
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