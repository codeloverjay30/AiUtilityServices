using AiUtility.GeminiUtilityServices.Services;
using FluentAssertions;
using System.Text.Json;

namespace AiUtility.GeminiUtilityServices.Tests;

/// <summary>
/// Contains regression tests for Gemini function-response normalization.
/// </summary>
public sealed class GeminiSessionManagerFunctionResponseTests
{
    [Fact]
    public void CreateFunctionResponse_WhenResultIsNull_ShouldReturnSuccessfulObject()
    {
        // Arrange
        object? result =
            null;

        // Act
        var response =
            GeminiSessionManager.CreateFunctionResponse(
                result);

        // Assert
        response.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        response.GetProperty(
                "success")
            .GetBoolean()
            .Should()
            .BeTrue();

        response.TryGetProperty(
                "result",
                out _)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CreateFunctionResponse_WhenResultIsObject_ShouldPreserveStructuredObject()
    {
        // Arrange
        var result =
            new
            {
                success = true,
                x = 100,
                y = 200,
            };

        // Act
        var response =
            GeminiSessionManager.CreateFunctionResponse(
                result);

        // Assert
        response.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        response.GetProperty(
                "success")
            .GetBoolean()
            .Should()
            .BeTrue();

        response.GetProperty(
                "x")
            .GetInt32()
            .Should()
            .Be(
                100);

        response.GetProperty(
                "y")
            .GetInt32()
            .Should()
            .Be(
                200);

        response.TryGetProperty(
                "result",
                out _)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CreateFunctionResponse_WhenResultIsString_ShouldWrapResultInSuccessfulObject()
    {
        // Arrange
        const string result =
            "completed";

        // Act
        var response =
            GeminiSessionManager.CreateFunctionResponse(
                result);

        // Assert
        AssertWrappedSuccessfulResponse(
            response);

        response.GetProperty(
                "result")
            .GetString()
            .Should()
            .Be(
                result);
    }

    [Fact]
    public void CreateFunctionResponse_WhenResultIsBoolean_ShouldWrapResultInSuccessfulObject()
    {
        // Arrange
        const bool result =
            true;

        // Act
        var response =
            GeminiSessionManager.CreateFunctionResponse(
                result);

        // Assert
        AssertWrappedSuccessfulResponse(
            response);

        response.GetProperty(
                "result")
            .GetBoolean()
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CreateFunctionResponse_WhenResultIsNumber_ShouldWrapResultInSuccessfulObject()
    {
        // Arrange
        const int result =
            42;

        // Act
        var response =
            GeminiSessionManager.CreateFunctionResponse(
                result);

        // Assert
        AssertWrappedSuccessfulResponse(
            response);

        response.GetProperty(
                "result")
            .GetInt32()
            .Should()
            .Be(
                result);
    }

    [Fact]
    public void CreateFunctionResponse_WhenResultIsArray_ShouldWrapResultInSuccessfulObject()
    {
        // Arrange
        int[] result =
        [
            1,
            2,
            3,
        ];

        // Act
        var response =
            GeminiSessionManager.CreateFunctionResponse(
                result);

        // Assert
        AssertWrappedSuccessfulResponse(
            response);

        var wrappedResult =
            response.GetProperty(
                "result");

        wrappedResult.ValueKind.Should()
            .Be(
                JsonValueKind.Array);

        wrappedResult.GetArrayLength()
            .Should()
            .Be(
                3);

        wrappedResult[0]
            .GetInt32()
            .Should()
            .Be(
                1);

        wrappedResult[1]
            .GetInt32()
            .Should()
            .Be(
                2);

        wrappedResult[2]
            .GetInt32()
            .Should()
            .Be(
                3);
    }

    [Fact]
    public void CreateFunctionResponse_WhenResultIsJsonElementObject_ShouldPreserveObject()
    {
        // Arrange
        var result =
            JsonSerializer.SerializeToElement(
                new
                {
                    status = "completed",
                    count = 2,
                });

        // Act
        var response =
            GeminiSessionManager.CreateFunctionResponse(
                result);

        // Assert
        response.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        response.GetProperty(
                "status")
            .GetString()
            .Should()
            .Be(
                "completed");

        response.GetProperty(
                "count")
            .GetInt32()
            .Should()
            .Be(
                2);

        response.TryGetProperty(
                "result",
                out _)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void CreateFunctionResponse_WhenResultIsJsonElementNull_ShouldWrapNullInSuccessfulObject()
    {
        // Arrange
        var result =
            JsonSerializer.SerializeToElement<object?>(
                null);

        // Act
        var response =
            GeminiSessionManager.CreateFunctionResponse(
                result);

        // Assert
        AssertWrappedSuccessfulResponse(
            response);

        response.GetProperty(
                "result")
            .ValueKind.Should()
            .Be(
                JsonValueKind.Null);
    }

    /// <summary>
    /// Verifies that a normalized response is a successful wrapper object.
    /// </summary>
    /// <param name="response">
    /// The response to verify.
    /// </param>
    private static void AssertWrappedSuccessfulResponse(
        JsonElement response)
    {
        response.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        response.GetProperty(
                "success")
            .GetBoolean()
            .Should()
            .BeTrue();

        response.TryGetProperty(
                "result",
                out _)
            .Should()
            .BeTrue();
    }
}