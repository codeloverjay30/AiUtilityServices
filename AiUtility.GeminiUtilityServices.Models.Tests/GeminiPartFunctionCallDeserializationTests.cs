using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models.Tests;

/// <summary>
/// Contains regression tests for deserializing Gemini function-call parts
/// from the Gemini API wire format.
/// </summary>
public sealed class GeminiPartFunctionCallDeserializationTests
{
    /// <summary>
    /// Verifies that the Gemini wire property "functionCall" is mapped to
    /// <see cref="GeminiPart.FunctionCall"/>.
    /// </summary>
    [Fact]
    public void Deserialize_FunctionCallWireProperty_ShouldPopulateFunctionCall()
    {
        // Arrange
        const string json =
            """
            {
              "functionCall": {
                "name": "Click",
                "args": {
                  "target": {
                    "description": "Activity entrance"
                  }
                }
              }
            }
            """;

        var options =
            CreateStrictJsonOptions();

        // Act
        var result =
            JsonSerializer.Deserialize<GeminiPart>(
                json,
                options);

        // Assert
        result.Should()
            .NotBeNull();

        result!.FunctionCall.Should()
            .NotBeNull();

        result.FunctionCall!.Name.Should()
            .Be(
                "Click");
    }

    /// <summary>
    /// Verifies that function-call arguments are preserved as JSON elements
    /// when a Gemini function call is deserialized.
    /// </summary>
    [Fact]
    public void Deserialize_FunctionCallWithComplexArgument_ShouldPreserveArgumentJson()
    {
        // Arrange
        const string json =
            """
            {
              "functionCall": {
                "name": "Click",
                "args": {
                  "target": {
                    "description": "Activity entrance"
                  }
                }
              }
            }
            """;

        var options =
            CreateStrictJsonOptions();

        // Act
        var result =
            JsonSerializer.Deserialize<GeminiPart>(
                json,
                options);

        // Assert
        result.Should()
            .NotBeNull();

        result!.FunctionCall.Should()
            .NotBeNull();

        result.FunctionCall!.Args.Should()
            .ContainKey(
                "target");

        var target =
            result.FunctionCall.Args[
                "target"];

        target.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        target.GetProperty(
                "description")
            .GetString()
            .Should()
            .Be(
                "Activity entrance");
    }

    /// <summary>
    /// Verifies that multiple function-call arguments are preserved without
    /// losing their JSON value types.
    /// </summary>
    [Fact]
    public void Deserialize_FunctionCallWithMultipleArguments_ShouldPreserveArgumentTypes()
    {
        // Arrange
        const string json =
            """
            {
              "functionCall": {
                "name": "Wait",
                "args": {
                  "milliseconds": 1500,
                  "reason": "Wait for animation",
                  "required": true
                }
              }
            }
            """;

        var options =
            CreateStrictJsonOptions();

        // Act
        var result =
            JsonSerializer.Deserialize<GeminiPart>(
                json,
                options);

        // Assert
        result.Should()
            .NotBeNull();

        result!.FunctionCall.Should()
            .NotBeNull();

        var functionCall =
            result.FunctionCall!;

        functionCall.Name.Should()
            .Be(
                "Wait");

        functionCall.Args.Should()
            .ContainKeys(
                "milliseconds",
                "reason",
                "required");

        functionCall.Args[
                "milliseconds"]
            .GetInt32()
            .Should()
            .Be(
                1500);

        functionCall.Args[
                "reason"]
            .GetString()
            .Should()
            .Be(
                "Wait for animation");

        functionCall.Args[
                "required"]
            .GetBoolean()
            .Should()
            .BeTrue();
    }

    /// <summary>
    /// Verifies that an unknown Gemini part property is rejected when strict
    /// unmapped-member handling is enabled.
    /// </summary>
    [Fact]
    public void Deserialize_UnknownWireProperty_ShouldThrowJsonException()
    {
        // Arrange
        const string json =
            """
            {
              "unexpectedGeminiProperty": {
                "value": "unexpected"
              }
            }
            """;

        var options =
            CreateStrictJsonOptions();

        // Act
        Action act =
            () =>
                JsonSerializer.Deserialize<GeminiPart>(
                    json,
                    options);

        // Assert
        act.Should()
            .Throw<JsonException>()
            .WithMessage(
                "*unexpectedGeminiProperty*");
    }

    /// <summary>
    /// Creates JSON serializer options that reject unknown wire properties
    /// so contract mismatches cannot be silently ignored.
    /// </summary>
    /// <returns>
    /// Strict JSON serializer options for Gemini wire-contract tests.
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
