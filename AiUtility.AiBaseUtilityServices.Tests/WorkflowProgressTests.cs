using AiUtility.AiBaseUtilityServices.Models;
using FluentAssertions;

namespace AiUtility.AiBaseUtilityServices.Tests;

/// <summary>
/// Provides regression tests for <see cref="WorkflowProgress"/>.
/// </summary>
public class WorkflowProgressTests
{
    /// <summary>
    /// Verifies that ToString includes all values required by the default format.
    /// </summary>
    [Fact]
    public void ToString_WhenAllProgressValuesAreProvided_ShouldReturnFormattedProgress()
    {
        // Arrange
        var sut = new WorkflowProgress
        {
            Percentage = 50,
            CurrentStep = 2,
            MaxSteps = 4,
            CurrentAction = "Executing tool"
        };

        // Act
        var result = sut.ToString();

        // Assert
        result.Should().Be("[50%] Step 2/4: Executing tool");
    }
}
