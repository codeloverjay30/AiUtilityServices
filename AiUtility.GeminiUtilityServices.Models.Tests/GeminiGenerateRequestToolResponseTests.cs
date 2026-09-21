using System.Text.Json;
using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;

namespace AiUtility.GeminiUtilityServices.Models.Tests;

/// <summary>
/// Provides regression tests for Gemini tool-response request serialization.
/// </summary>
public class GeminiGenerateRequestToolResponseTests
{
    /// <summary>
    /// Verifies that the string overload of AddToolResponse serializes the
    /// function response as user content required by the Gemini API.
    /// </summary>
    [Fact]
    public void AddToolResponse_StringOverload_ShouldSerializeToolResponseWithUserRole()
    {
        // Arrange
        var request = new GeminiGenerateRequest();

#pragma warning disable CS0618
        request.AddToolResponse(
            "Wait",
            """{"success":true}""");
#pragma warning restore CS0618

        // Act
        JsonElement content = GetLastWireContent(request);

        // Assert
        content.GetProperty("role")
            .GetString()
            .Should()
            .Be("user");

        JsonElement functionResponse =
            content.GetProperty("parts")[0]
                .GetProperty("function_response");

        functionResponse.GetProperty("name")
            .GetString()
            .Should()
            .Be("Wait");

        functionResponse.GetProperty("response")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Object);
    }

    /// <summary>
    /// Verifies that the memory overload of AddToolResponse serializes the
    /// function response as user content required by the Gemini API.
    /// </summary>
    [Fact]
    public void AddToolResponse_MemoryOverload_ShouldSerializeToolResponseWithUserRole()
    {
        // Arrange
        var request = new GeminiGenerateRequest();

        // Act
        request.AddToolResponse(
            "Wait".AsMemory(),
            """{"success":true}""".AsMemory());

        JsonElement content = GetLastWireContent(request);

        // Assert
        content.GetProperty("role")
            .GetString()
            .Should()
            .Be("user");

        JsonElement functionResponse =
            content.GetProperty("parts")[0]
                .GetProperty("function_response");

        functionResponse.GetProperty("name")
            .GetString()
            .Should()
            .Be("Wait");

        functionResponse.GetProperty("response")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Object);
    }

    /// <summary>
    /// Verifies that the string overload of WithToolResponse serializes the
    /// function response as user content without mutating the source request.
    /// </summary>
    [Fact]
    public void WithToolResponse_StringOverload_ShouldSerializeToolResponseWithUserRole()
    {
        // Arrange
        var request = new GeminiGenerateRequest();

        // Act
#pragma warning disable CS0618
        GeminiGenerateRequest clone =
            request.WithToolResponse(
                "Click",
                """{"success":true}""");
#pragma warning restore CS0618

        JsonElement content = GetLastWireContent(clone);

        // Assert
        request.Contents.Should().BeEmpty();

        clone.Contents.Should()
            .HaveCount(1);

        content.GetProperty("role")
            .GetString()
            .Should()
            .Be("user");

        JsonElement functionResponse =
            content.GetProperty("parts")[0]
                .GetProperty("function_response");

        functionResponse.GetProperty("name")
            .GetString()
            .Should()
            .Be("Click");

        functionResponse.GetProperty("response")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Object);
    }

    /// <summary>
    /// Verifies that the memory overload of WithToolResponse serializes the
    /// function response as user content without mutating the source request.
    /// </summary>
    [Fact]
    public void WithToolResponse_MemoryOverload_ShouldSerializeToolResponseWithUserRole()
    {
        // Arrange
        var request = new GeminiGenerateRequest();

        // Act
        GeminiGenerateRequest clone =
            request.WithToolResponse(
                "Click".AsMemory(),
                """{"success":true}""".AsMemory());

        JsonElement content = GetLastWireContent(clone);

        // Assert
        request.Contents.Should().BeEmpty();

        clone.Contents.Should()
            .HaveCount(1);

        content.GetProperty("role")
            .GetString()
            .Should()
            .Be("user");

        JsonElement functionResponse =
            content.GetProperty("parts")[0]
                .GetProperty("function_response");

        functionResponse.GetProperty("name")
            .GetString()
            .Should()
            .Be("Click");

        functionResponse.GetProperty("response")
            .ValueKind
            .Should()
            .Be(JsonValueKind.Object);
    }

    /// <summary>
    /// Serializes the request through its Google API wire representation and
    /// returns the final content element.
    /// </summary>
    /// <param name="request">The Gemini request to serialize.</param>
    /// <returns>A detached JSON element representing the final content.</returns>
    private static JsonElement GetLastWireContent(
        GeminiGenerateRequest request)
    {
        object wireRequest =
            request.ToGoogleApiRequest();

        string json =
            JsonSerializer.Serialize(wireRequest);

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement contents =
            document.RootElement.GetProperty("contents");

        contents.GetArrayLength()
            .Should()
            .BeGreaterThan(0);

        return contents[contents.GetArrayLength() - 1]
            .Clone();
    }
}