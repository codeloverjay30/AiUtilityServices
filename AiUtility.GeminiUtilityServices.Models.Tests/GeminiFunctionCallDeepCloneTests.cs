using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;
using System.Text.Json;

namespace AiUtility.GeminiUtilityServices.Models.Tests;

/// <summary>
/// Contains regression tests for deep cloning Gemini function calls.
/// </summary>
public sealed class GeminiFunctionCallDeepCloneTests
{
    /// <summary>
    /// Verifies that a deep clone preserves the function-call identifier
    /// and function name.
/// </summary>
    [Fact]
    public void DeepClone_WithIdAndName_ShouldPreserveScalarProperties()
    {
        // Arrange
        var source =
            new GeminiFunctionCall
            {
                Id = "function-call-001",
                Name = "Click",
            };

        // Act
        var clone =
            source.DeepClone();

        // Assert
        clone.Should()
            .NotBeSameAs(
                source);

        clone.Id.Should()
            .Be(
                "function-call-001");

        clone.Name.Should()
            .Be(
                "Click");
    }

    /// <summary>
    /// Verifies that a deep clone creates an independent arguments
    /// dictionary instead of sharing the source dictionary.
    /// </summary>
    [Fact]
    public void DeepClone_WithArguments_ShouldCreateIndependentArgumentsDictionary()
    {
        // Arrange
        var source =
            new GeminiFunctionCall
            {
                Id = "function-call-002",
                Name = "Wait",
                Args =
                    new Dictionary<string, JsonElement>
                    {
                        ["milliseconds"] =
                            CreateJsonElement(
                                "1500"),
                    },
            };

        // Act
        var clone =
            source.DeepClone();

        // Assert
        clone.Args.Should()
            .NotBeSameAs(
                source.Args);

        clone.Args.Should()
            .ContainKey(
                "milliseconds");

        clone.Args["milliseconds"]
            .GetInt32()
            .Should()
            .Be(
                1500);
    }

    /// <summary>
    /// Verifies that modifying the cloned arguments dictionary does not
    /// modify the source function call.
    /// </summary>
    [Fact]
    public void DeepClone_WhenCloneArgumentsAreModified_ShouldNotModifySourceArguments()
    {
        // Arrange
        var source =
            new GeminiFunctionCall
            {
                Id = "function-call-003",
                Name = "Wait",
                Args =
                    new Dictionary<string, JsonElement>
                    {
                        ["milliseconds"] =
                            CreateJsonElement(
                                "1500"),
                    },
            };

        var clone =
            source.DeepClone();

        // Act
        clone.Args["milliseconds"] =
            CreateJsonElement(
                "3000");

        clone.Args["reason"] =
            CreateJsonElement(
                "\"Wait for animation\"");

        // Assert
        source.Args.Should()
            .ContainSingle();

        source.Args.Should()
            .ContainKey(
                "milliseconds");

        source.Args.Should()
            .NotContainKey(
                "reason");

        source.Args["milliseconds"]
            .GetInt32()
            .Should()
            .Be(
                1500);

        clone.Args["milliseconds"]
            .GetInt32()
            .Should()
            .Be(
                3000);
    }

    /// <summary>
    /// Verifies that a deep clone preserves a complex JSON argument after
    /// the source JSON document has been disposed.
    /// </summary>
    [Fact]
    public void DeepClone_WithComplexArgument_ShouldPreserveIndependentJsonElement()
    {
        // Arrange
        var source =
            new GeminiFunctionCall
            {
                Id = "function-call-004",
                Name = "Click",
            };

        GeminiFunctionCall clone;

        using (
            var document =
                JsonDocument.Parse(
                    """
                    {
                      "description": "Login button",
                      "position": {
                        "x": 100,
                        "y": 200
                      }
                    }
                    """))
        {
            source.Args["target"] =
                document.RootElement;

            // Act
            clone =
                source.DeepClone();
        }

        // Assert
        clone.Args.Should()
            .ContainKey(
                "target");

        var target =
            clone.Args["target"];

        target.ValueKind.Should()
            .Be(
                JsonValueKind.Object);

        target.GetProperty(
                "description")
            .GetString()
            .Should()
            .Be(
                "Login button");

        var position =
            target.GetProperty(
                "position");

        position.GetProperty(
                "x")
            .GetInt32()
            .Should()
            .Be(
                100);

        position.GetProperty(
                "y")
            .GetInt32()
            .Should()
            .Be(
                200);
    }

    /// <summary>
    /// Verifies that a deep clone preserves a missing optional identifier.
    /// </summary>
    [Fact]
    public void DeepClone_WithoutId_ShouldPreserveNullId()
    {
        // Arrange
        var source =
            new GeminiFunctionCall
            {
                Id = null,
                Name = "Wait",
            };

        // Act
        var clone =
            source.DeepClone();

        // Assert
        clone.Should()
            .NotBeSameAs(
                source);

        clone.Id.Should()
            .BeNull();

        clone.Name.Should()
            .Be(
                "Wait");
    }

    /// <summary>
    /// Creates an independent JSON element from the supplied JSON text.
    /// </summary>
    /// <param name="json">
    /// The JSON text to parse.
    /// </param>
    /// <returns>
    /// An independent clone of the parsed JSON element.
    /// </returns>
    private static JsonElement CreateJsonElement(
        string json)
    {
        using var document =
            JsonDocument.Parse(
                json);

        return document.RootElement.Clone();
    }
}