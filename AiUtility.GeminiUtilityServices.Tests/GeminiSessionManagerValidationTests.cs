using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using CommonModels;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Moq;
using ThreadLevelLockingUtilityServices;

namespace AiUtility.GeminiUtilityServices.Tests;

public sealed class GeminiSessionManagerValidationTests
{
    [Theory]
    [InlineData(false, "request")]
    [InlineData(true, "request")]
    [InlineData(false, "settings")]
    [InlineData(true, "settings")]
    [InlineData(false, "userTask")]
    [InlineData(true, "userTask")]
    [InlineData(false, "zeroSteps")]
    [InlineData(true, "zeroSteps")]
    [InlineData(false, "negativeSteps")]
    [InlineData(true, "negativeSteps")]
    [InlineData(false, "zeroTimeout")]
    [InlineData(true, "zeroTimeout")]
    [InlineData(false, "negativeTimeout")]
    [InlineData(true, "negativeTimeout")]
    public void ExecuteWithToolSupport_InvalidInput_ThrowsBeforeUsingDependencies(
        bool useWithPath, string invalidInput)
    {
        var manager = CreateManager();
        var request = new GeminiGenerateRequest();
        var settings = new AiExecutionSettings();
        var task = "Validate input".AsMemory();

        switch (invalidInput)
        {
            case "request": request = null!; break;
            case "settings": settings = null!; break;
            case "userTask": task = ReadOnlyMemory<char>.Empty; break;
            case "zeroSteps": settings.MaxSteps = 0; break;
            case "negativeSteps": settings.MaxSteps = -1; break;
            case "zeroTimeout": settings.ToolExecutionTimeout = TimeSpan.Zero; break;
            case "negativeTimeout":
                settings.ToolExecutionTimeout = TimeSpan.FromMilliseconds(-1);
                break;
        }

        Action act = () =>
        {
            var execution = useWithPath
                ? manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(request, task, settings)
                : manager.ExecuteWithToolSupportAsync<WorkflowProgress>(request, task, settings);
            execution.GetAwaiter().GetResult();
        };

        if (invalidInput is "request" or "settings")
        {
            act.Should().ThrowExactly<ArgumentNullException>()
                .WithMessage($"*{invalidInput}*")
                .Which.ParamName.Should().Be(invalidInput);
        }
        else if (invalidInput == "userTask")
        {
            act.Should().ThrowExactly<ArgumentException>()
                .WithMessage("User task cannot be empty.*")
                .Which.ParamName.Should().Be("userTask");
        }
        else
        {
            var parameter = invalidInput.EndsWith("Steps") ? "MaxSteps" : "ToolExecutionTimeout";
            act.Should().ThrowExactly<ArgumentOutOfRangeException>()
                .WithMessage($"*{parameter}*")
                .Which.ParamName.Should().Be(parameter);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExecuteWithToolSupport_NullStringTask_ThrowsArgumentNullException(bool useWithPath)
    {
        var manager = CreateManager();
        var request = new GeminiGenerateRequest();
        var settings = new AiExecutionSettings();

#pragma warning disable CS0618 // Exercise the public legacy string overloads.
        Action act = () =>
        {
            var execution = useWithPath
                ? manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(request, (string)null!, settings)
                : manager.ExecuteWithToolSupportAsync<WorkflowProgress>(request, (string)null!, settings);
            execution.GetAwaiter().GetResult();
        };
#pragma warning restore CS0618

        act.Should().ThrowExactly<ArgumentNullException>()
            .WithMessage("*userTask*")
            .Which.ParamName.Should().Be("userTask");
    }

    private static GeminiSessionManager CreateManager()
    {
        // Strict mocks ensure validation does not reach any collaborator.
        return new GeminiSessionManager(
            new Mock<ILoggerFactoryBaseUtilityService>(MockBehavior.Strict).Object,
            new Mock<IGeminiConversationManager>(MockBehavior.Strict).Object,
            new Mock<IGeminiToolService>(MockBehavior.Strict).Object,
            new Mock<IGeminiToolExecutor>(MockBehavior.Strict).Object,
            new Mock<ISemaphoreSlimService>(MockBehavior.Strict).Object);
    }
}