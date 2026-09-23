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

public sealed class GeminiSessionManagerEmptyResponseTests
{
    [Theory]
    [InlineData(false, "nullResponse")]
    [InlineData(true, "nullResponse")]
    [InlineData(false, "nullCandidates")]
    [InlineData(true, "nullCandidates")]
    [InlineData(false, "emptyCandidates")]
    [InlineData(true, "emptyCandidates")]
    [InlineData(false, "nullContent")]
    [InlineData(true, "nullContent")]
    [InlineData(false, "nullParts")]
    [InlineData(true, "nullParts")]
    [InlineData(false, "emptyParts")]
    [InlineData(true, "emptyParts")]
    public async Task EmptyResponse_ReturnsFailureAndErrorProgress(bool useWithPath, string shape)
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

        var content = new GeminiMessage { Parts = new List<GeminiPart>() };
        var candidate = new GeminiCandidate { Content = content };
        GeminiResponse? response = new()
        {
            Candidates = new List<GeminiCandidate> { candidate }
        };
        switch (shape)
        {
            case "nullResponse": response = null; break;
            case "nullCandidates": response.Candidates = null!; break;
            case "emptyCandidates": response.Candidates.Clear(); break;
            case "nullContent": candidate.Content = null!; break;
            case "nullParts": content.Parts = null!; break;
        }

        var calls = 0;
        if (useWithPath)
        {
            conversation.Setup(x => x.WithSendMessageAsync(
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
        var settings = new AiExecutionSettings { MaxSteps = 2 };
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
        status.ErrorMessage.Should().Be(Constants.Messages.FailureMessages.AI_RETURNS_NULL_RESPONSE);
        status.OverallErrorMessage.Should().Be(status.ErrorMessage);
        status.DetailedErrorMessage.Should().Be(status.ErrorMessage);
        status.Metadata.Should().Contain("DeviceId", "test-device");
        reports.Should().NotBeEmpty();
        var last = reports.Last();
        last.CurrentAction.Should().Be(Constants.ExecutionStatus.ERROR);
        last.Percentage.Should().Be(0);
        last.CurrentStep.Should().Be(1);
        last.MaxSteps.Should().Be(2);
        last.Metadata.Should().Contain("DeviceId", "test-device");
    }
}