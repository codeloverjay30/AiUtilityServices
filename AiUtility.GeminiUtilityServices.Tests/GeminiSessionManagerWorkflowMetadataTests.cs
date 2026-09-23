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

public sealed class GeminiSessionManagerWorkflowMetadataTests
{
    [Theory]
    [InlineData(false, "success")]
    [InlineData(true, "success")]
    [InlineData(false, "empty")]
    [InlineData(true, "empty")]
    [InlineData(false, "exception")]
    [InlineData(true, "exception")]
    [InlineData(false, "unsupported")]
    [InlineData(true, "unsupported")]
    public async Task Workflow_ReportsStatusWithoutMutatingSourceMetadata(bool useWithPath, string outcome)
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

        Task<GeminiResponse> Respond() => outcome == "exception"
            ? Task.FromException<GeminiResponse>(new InvalidOperationException("Model unavailable"))
            : Task.FromResult(outcome == "empty" ? null! : new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new() { Content = new GeminiMessage
                    {
                        Role = "model",
                        Parts = new List<GeminiPart>
                        {
                            new() { Text = outcome == "success" ? "Done" : null }
                        }
                    }}
                }
            });
        var calls = 0;
        if (useWithPath)
        {
            conversation.Setup(x => x.WithSendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => calls++)
                .Returns(Respond);
        }
        else
        {
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => calls++)
                .Returns(Respond);
        }

        var reports = new List<WorkflowProgress>();
        var progress = new Mock<IProgress<WorkflowProgress>>();
        progress.Setup(x => x.Report(It.IsAny<WorkflowProgress>()))
            .Callback<WorkflowProgress>(reports.Add);
        var settings = new AiExecutionSettings { MaxSteps = 2 };
        settings.Metadata["DeviceId"] = "test-device";
        settings.Metadata["WorkflowStatus"] = "source-workflow";
        settings.Metadata["TaskStatus"] = "source-task";
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
        var expected = outcome == "success" ? "Completed" : "Failed";
        status.IsSuccess.Should().Be(outcome == "success");
        status.Metadata.Should().Contain("DeviceId", "test-device")
            .And.Contain("WorkflowStatus", expected).And.Contain("TaskStatus", "Unknown");
        reports.Should().NotBeEmpty();
        reports.First().Metadata.Should().Contain("WorkflowStatus", "InProgress")
            .And.Contain("TaskStatus", "Unknown");
        reports.Last().Metadata.Should().Contain("WorkflowStatus", expected)
            .And.Contain("TaskStatus", "Unknown");
        reports.First().Metadata.Should().NotBeSameAs(reports.Last().Metadata);
        status.Metadata.Should().NotBeSameAs(settings.Metadata);
        settings.Metadata.Should().Contain("WorkflowStatus", "source-workflow")
            .And.Contain("TaskStatus", "source-task");
        status.Metadata["DeviceId"] = "changed-result";
        settings.Metadata.Should().Contain("DeviceId", "test-device");
        reports.Last().Metadata.Should().Contain("DeviceId", "test-device");
    }
}