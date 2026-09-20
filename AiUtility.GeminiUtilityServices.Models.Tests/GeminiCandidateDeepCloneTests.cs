using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;

namespace AiUtility.GeminiUtilityServices.Models.Tests;

/// <summary>
/// Contains regression tests for deep cloning Gemini candidates.
/// </summary>
public sealed class GeminiCandidateDeepCloneTests
{
    /// <summary>
    /// Verifies that a deep clone preserves all scalar candidate properties.
    /// </summary>
    [Fact]
    public void DeepClone_WithScalarProperties_ShouldPreserveValues()
    {
        // Arrange
        var source =
            new GeminiCandidate
            {
                FinishMessage = "Generation completed.",
                FinishReason = "STOP",
                Index = 2,
                AverageLogProbabilities = -0.125,
            };

        // Act
        var clone =
            source.DeepClone();

        // Assert
        clone.Should()
            .NotBeSameAs(
                source);

        clone.FinishMessage.Should()
            .Be(
                "Generation completed.");

        clone.FinishReason.Should()
            .Be(
                "STOP");

        clone.Index.Should()
            .Be(
                2);

        clone.AverageLogProbabilities.Should()
            .Be(
                -0.125);
    }

    /// <summary>
    /// Verifies that a deep clone creates an independent content instance.
    /// </summary>
    [Fact]
    public void DeepClone_WithContent_ShouldCreateIndependentContent()
    {
        // Arrange
        var source =
            new GeminiCandidate
            {
                Content =
                    new GeminiMessage
                    {
                        Role = "model",
                        Parts =
                        [
                            new GeminiPart
                            {
                                Text = "Original response",
                            },
                        ],
                    },
                FinishMessage = "Generation completed.",
                FinishReason = "STOP",
                Index = 0,
            };

        // Act
        var clone =
            source.DeepClone();

        // Assert
        clone.Content.Should()
            .NotBeSameAs(
                source.Content);

        clone.Content.Role.Should()
            .Be(
                "model");

        clone.Content.Parts.Should()
            .ContainSingle();

        clone.Content.Parts[0]
            .Text.Should()
            .Be(
                "Original response");
    }

    /// <summary>
    /// Verifies that a deep clone creates an independent parts collection
    /// and independent part instances.
    /// </summary>
    [Fact]
    public void DeepClone_WithParts_ShouldCreateIndependentParts()
    {
        // Arrange
        var source =
            new GeminiCandidate
            {
                Content =
                    new GeminiMessage
                    {
                        Role = "model",
                        Parts =
                        [
                            new GeminiPart
                            {
                                Text = "First part",
                            },
                            new GeminiPart
                            {
                                Text = "Second part",
                            },
                        ],
                    },
            };

        // Act
        var clone =
            source.DeepClone();

        // Assert
        clone.Content.Parts.Should()
            .NotBeSameAs(
                source.Content.Parts);

        clone.Content.Parts.Should()
            .HaveCount(
                2);

        clone.Content.Parts[0].Should()
            .NotBeSameAs(
                source.Content.Parts[0]);

        clone.Content.Parts[1].Should()
            .NotBeSameAs(
                source.Content.Parts[1]);

        clone.Content.Parts[0]
            .Text.Should()
            .Be(
                "First part");

        clone.Content.Parts[1]
            .Text.Should()
            .Be(
                "Second part");
    }

    /// <summary>
    /// Verifies that modifying cloned nested content does not modify the
    /// source candidate.
    /// </summary>
    [Fact]
    public void DeepClone_WhenCloneIsModified_ShouldNotModifySource()
    {
        // Arrange
        var source =
            new GeminiCandidate
            {
                Content =
                    new GeminiMessage
                    {
                        Role = "model",
                        Parts =
                        [
                            new GeminiPart
                            {
                                Text = "Original response",
                            },
                        ],
                    },
                FinishMessage = "Original finish message",
                FinishReason = "STOP",
                Index = 0,
            };

        var clone =
            source.DeepClone();

        // Act
        clone.Content.Role =
            "modified-role";

        clone.Content.Parts[0].Text =
            "Modified response";

        clone.Content.Parts.Add(
            new GeminiPart
            {
                Text = "Additional response",
            });

        clone.FinishMessage =
            "Modified finish message";

        // Assert
        source.Content.Role.Should()
            .Be(
                "model");

        source.Content.Parts.Should()
            .ContainSingle();

        source.Content.Parts[0]
            .Text.Should()
            .Be(
                "Original response");

        source.FinishMessage.Should()
            .Be(
                "Original finish message");

        clone.Content.Role.Should()
            .Be(
                "modified-role");

        clone.Content.Parts.Should()
            .HaveCount(
                2);

        clone.FinishMessage.Should()
            .Be(
                "Modified finish message");
    }

    /// <summary>
    /// Verifies that a deep clone preserves null optional candidate
    /// properties.
    /// </summary>
    [Fact]
    public void DeepClone_WithNullOptionalProperties_ShouldPreserveNullValues()
    {
        // Arrange
        var source =
            new GeminiCandidate
            {
                FinishMessage = null,
                FinishReason = null,
                Index = null,
                AverageLogProbabilities = null,
            };

        // Act
        var clone =
            source.DeepClone();

        // Assert
        clone.Should()
            .NotBeSameAs(
                source);

        clone.FinishMessage.Should()
            .BeNull();

        clone.FinishReason.Should()
            .BeNull();

        clone.Index.Should()
            .BeNull();

        clone.AverageLogProbabilities.Should()
            .BeNull();
    }
}
