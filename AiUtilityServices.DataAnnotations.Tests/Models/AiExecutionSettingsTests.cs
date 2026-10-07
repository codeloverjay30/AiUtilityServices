using AiUtility.AiBaseUtilityServices.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace AiUtilityServices.DataAnnotations.Tests.Models;

/// <summary>
/// Provides validation tests for <see cref="AiExecutionSettings"/>.
/// </summary>
[TestFixture]
public sealed class AiExecutionSettingsTests
{
    /// <summary>
    /// Verifies that the default execution settings satisfy all
    /// DataAnnotations validation constraints.
    /// </summary>
    [Test]
    public void Validation_ShouldSucceed_WhenUsingDefaultSettings()
    {
        // Arrange
        AiExecutionSettings sut = new();

        // Act
        List<ValidationResult> validationResults = Validate(sut);

        // Assert
        validationResults.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a positive tool execution timeout is valid.
    /// </summary>
    [Test]
    public void Validation_ShouldSucceed_WhenToolExecutionTimeoutIsPositive()
    {
        // Arrange
        AiExecutionSettings sut = new()
        {
            ToolExecutionTimeout = TimeSpan.FromTicks(1)
        };

        // Act
        List<ValidationResult> validationResults = Validate(sut);

        // Assert
        validationResults.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a zero tool execution timeout is rejected.
    /// </summary>
    [Test]
    public void Validation_ShouldFail_WhenToolExecutionTimeoutIsZero()
    {
        // Arrange
        AiExecutionSettings sut = new()
        {
            ToolExecutionTimeout = TimeSpan.Zero
        };

        // Act
        List<ValidationResult> validationResults = Validate(sut);

        // Assert
        validationResults.Should()
            .ContainSingle(result =>
                result.MemberNames.Contains(
                    nameof(AiExecutionSettings.ToolExecutionTimeout)));
    }

    /// <summary>
    /// Verifies that a negative tool execution timeout is rejected.
    /// </summary>
    [Test]
    public void Validation_ShouldFail_WhenToolExecutionTimeoutIsNegative()
    {
        // Arrange
        AiExecutionSettings sut = new()
        {
            ToolExecutionTimeout = TimeSpan.FromTicks(-1)
        };

        // Act
        List<ValidationResult> validationResults = Validate(sut);

        // Assert
        validationResults.Should()
            .ContainSingle(result =>
                result.MemberNames.Contains(
                    nameof(AiExecutionSettings.ToolExecutionTimeout)));
    }

    /// <summary>
    /// Validates the specified execution settings through the standard
    /// DataAnnotations validation pipeline.
    /// </summary>
    /// <param name="settings">The execution settings to validate.</param>
    /// <returns>The validation failures produced by DataAnnotations.</returns>
    private static List<ValidationResult> Validate(
        AiExecutionSettings settings)
    {
        List<ValidationResult> validationResults = [];

        Validator.TryValidateObject(
            settings,
            new ValidationContext(settings),
            validationResults,
            validateAllProperties: true);

        return validationResults;
    }
}