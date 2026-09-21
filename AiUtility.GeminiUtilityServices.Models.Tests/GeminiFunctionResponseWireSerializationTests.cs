using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;
using System.Text.Json;

namespace AiUtility.GeminiUtilityServices.Models.Tests;

public sealed class GeminiFunctionResponseWireSerializationTests
{
    [Fact]
    public void Serialize_ResponseContainingJsonObject_ShouldProduceObjectValue()
    {
        // Arrange
        var source =
            new GeminiFunctionResponse
            {
                Name = "Click",
                Response =
                    JsonSerializer.SerializeToElement(
                        new
                        {
                            success = true,
                        }),
            };

        // Act
        var json =
            JsonSerializer.Serialize(
                source);

        using var document =
            JsonDocument.Parse(
                json);

        var root =
            document.RootElement;

        // Assert
        root.GetProperty(
                "name")
            .GetString()
            .Should()
            .Be(
                "Click");

        var response =
            root.GetProperty(
                "response");

        response.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        response.GetProperty(
                "success")
            .GetBoolean()
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Serialize_RawResponseContainingJsonObject_ShouldProduceObjectValue()
    {
        // Arrange
        var source =
            new GeminiFunctionResponse
            {
                RawName =
                    "Click".AsMemory(),

                RawResponse =
                    """{"success":true}"""
                        .AsMemory(),
            };

        // Act
        var json =
            JsonSerializer.Serialize(
                source);

        using var document =
            JsonDocument.Parse(
                json);

        var root =
            document.RootElement;

        // Assert
        root.TryGetProperty(
                "RawResponse",
                out _)
            .Should()
            .BeFalse();

        root.TryGetProperty(
                "RawName",
                out _)
            .Should()
            .BeFalse();

        root.GetProperty(
                "name")
            .GetString()
            .Should()
            .Be(
                "Click");

        var response =
            root.GetProperty(
                "response");

        response.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        response.GetProperty(
                "success")
            .GetBoolean()
            .Should()
            .BeTrue();
    }

    [Fact]
    public void RawResponse_WhenAssigned_ShouldUpdateStructuredResponse()
    {
        // Arrange
        var source =
            new GeminiFunctionResponse();

        var rawResponse =
            """{"status":"success"}"""
                .AsMemory();

        // Act
        source.RawResponse =
            rawResponse;

        // Assert
        source.Response.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        source.Response
            .GetProperty(
                "status")
            .GetString()
            .Should()
            .Be(
                "success");

        source.RawResponse
            .ToString()
            .Should()
            .Be(
                """{"status":"success"}""");
    }

    [Fact]
    public void RawResponse_WhenJsonIsNull_ShouldThrowArgumentException()
    {
        // Arrange
        var source =
            new GeminiFunctionResponse();

        // Act
        Action act =
            () => source.RawResponse =
                "null".AsMemory();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "*JSON object*");
    }

    [Fact]
    public void RawResponse_WhenJsonIsString_ShouldThrowArgumentException()
    {
        // Arrange
        var source =
            new GeminiFunctionResponse();

        // Act
        Action act =
            () => source.RawResponse =
                "\"hello\"".AsMemory();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "*JSON object*");
    }

    [Fact]
    public void RawResponse_WhenJsonIsInvalid_ShouldThrowArgumentException()
    {
        // Arrange
        var source =
            new GeminiFunctionResponse();

        // Act
        Action act =
            () => source.RawResponse =
                "{ invalid".AsMemory();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "*invalid JSON*");
    }
}