using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using CommonModels;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ThreadLevelLockingUtilityServices;

namespace AiUtility.GeminiUtilityServices.Tests;

public sealed class GeminiSessionManagerUnsupportedResponseTests
{
    [Theory]
    [InlineData(false, 1, null)]
    [InlineData(true, 1, null)]
    [InlineData(false, 2, null)]
    [InlineData(true, 2, null)]
    [InlineData(false, 1, "")]
    [InlineData(true, 1, "")]
    [InlineData(false, 2, "")]
    [InlineData(true, 2, "")]
    public async Task UnsupportedResponse_ReturnsFailureAndErrorProgress(
        bool useWithPath, int maxSteps, string? text)
    {
        var logger = new Mock<ILoggerFactoryBaseUtilityService>(MockBehavior.Strict);
        logger.SetupGet(x => x.Logger).Returns(NullLogger.Instance);
        var conversation = new Mock<IGeminiConversationManager>(MockBehavior.Strict);
        conversation.SetupGet(x => x.LastTotalTokens).Returns(0);
        var tools = new Mock<IGeminiToolService>(MockBehavior.Strict);
        tools.Setup(x => x.SyncToolsToRequest(It.IsAny<GeminiGenerateRequest>()));
        var executor = new Mock<IGeminiToolExecutor>(MockBehavior.Strict);
        var semaphore = new Mock<ISemaphoreSlimService>(MockBehavior.Strict);
        semaphore.Setup(x => x.LockWithTimeoutValueAsync(
                It.IsAny<CancellationToken>(), It.IsAny<TimeSpan>(), false))
            .ReturnsAsync(Mock.Of<IDisposable>());

        var response = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>
            {
                new()
                {
                    Content = new GeminiMessage
                    {
                        Role = "model",
                        Parts = new List<GeminiPart> { new() { Text = text } }
                    }
                }
            }
        };

        var calls = 0;
        if (useWithPath)
        {
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => calls++)
                .ReturnsAsync(response!);
        }
        else
        {
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => calls++)
                .ReturnsAsync(response!);
        }

        var reports = new List<WorkflowProgress>();
        var progress = new Mock<IProgress<WorkflowProgress>>();
        progress.Setup(x => x.Report(It.IsAny<WorkflowProgress>()))
            .Callback<WorkflowProgress>(reports.Add);
        var settings = new AiExecutionSettings { MaxSteps = maxSteps };
        settings.Metadata["DeviceId"] = "test-device";
        var manager = new GeminiSessionManager(
            logger.Object, conversation.Object, tools.Object, executor.Object, semaphore.Object);

        var result = useWithPath
            ? await manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(
                new GeminiGenerateRequest(), "Run task".AsMemory(), settings,
                CancellationToken.None, progress.Object)
            : await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                new GeminiGenerateRequest(), "Run task".AsMemory(), settings,
                CancellationToken.None, progress.Object);

        calls.Should().Be(1);
        executor.Invocations.Should().BeEmpty();
        var status = result.StatusList.Should().ContainSingle().Subject;
        status.IsSuccess.Should().BeFalse();
        status.Result.Should().Be(Constants.ExecutionStatus.ERROR);
        status.ErrorMessage.Should().Be("Gemini returned neither a function call nor textual content.");
        status.OverallErrorMessage.Should().Be(status.ErrorMessage);
        status.DetailedErrorMessage.Should().Be(status.ErrorMessage);
        status.Metadata.Should().Contain("DeviceId", "test-device");
        reports.Should().NotBeEmpty();
        var last = reports.Last();
        last.CurrentAction.Should().Be(Constants.ExecutionStatus.ERROR);
        last.Percentage.Should().Be(100 / maxSteps);
        last.CurrentStep.Should().Be(1);
        last.MaxSteps.Should().Be(maxSteps);
        last.Metadata.Should().Contain("DeviceId", "test-device");
    }
}