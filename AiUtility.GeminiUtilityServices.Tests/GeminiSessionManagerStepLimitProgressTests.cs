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

public sealed class GeminiSessionManagerStepLimitProgressTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(false, Constants.ExecutionSettings.MAX_STEPS + 5)]
    [InlineData(true, Constants.ExecutionSettings.MAX_STEPS + 5)]
    public async Task StepLimit_ReportsTerminalErrorProgress(bool useWithPath, int requestedSteps)
    {
        var expectedSteps = Math.Min(requestedSteps, Constants.ExecutionSettings.MAX_STEPS);
        var logger = new Mock<ILoggerFactoryBaseUtilityService>(MockBehavior.Strict);
        logger.SetupGet(x => x.Logger).Returns(NullLogger.Instance);
        var conversation = new Mock<IGeminiConversationManager>(MockBehavior.Strict);
        conversation.SetupGet(x => x.LastTotalTokens).Returns(0);
        var tools = new Mock<IGeminiToolService>(MockBehavior.Strict);
        tools.Setup(x => x.SyncToolsToRequest(It.IsAny<GeminiGenerateRequest>()));
        var semaphore = new Mock<ISemaphoreSlimService>(MockBehavior.Strict);
        semaphore.SetupGet(x => x.MaxRequestsPerWindow).Returns(1);
        semaphore.Setup(x => x.LockWithTimeoutValueAsync(
                It.IsAny<CancellationToken>(), It.IsAny<TimeSpan>(), false))
            .ReturnsAsync(Mock.Of<IDisposable>());

        var toolCalls = 0;
        var executor = new Mock<IGeminiToolExecutor>(MockBehavior.Strict);
        executor.Setup(x => x.ExecuteAsync(
                "Repeat", It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .Callback(() => toolCalls++)
            .ReturnsAsync(new { status = "ok" });

        var response = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>
            {
                new()
                {
                    Content = new GeminiMessage
                    {
                        Role = "model",
                        Parts = new List<GeminiPart>
                        {
                            new() { FunctionCall = new GeminiFunctionCall { Name = "Repeat" } }
                        }
                    }
                }
            }
        };
        var modelCalls = 0;
        if (useWithPath)
        {
            conversation.Setup(x => x.WithSendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => modelCalls++)
                .ReturnsAsync(response);
        }
        else
        {
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => modelCalls++)
                .ReturnsAsync(response);
        }

        var reports = new List<WorkflowProgress>();
        var progressLimits = new List<int>();
        var progress = new Mock<IProgress<WorkflowProgress>>();
        progress.Setup(x => x.Report(It.IsAny<WorkflowProgress>()))
            .Callback<WorkflowProgress>(report => { reports.Add(report); progressLimits.Add(report.MaxSteps); });
        var settings = new AiExecutionSettings
        {
            MaxSteps = requestedSteps,
            ForceSequentialToolExecution = true
        };
        settings.Metadata["DeviceId"] = "test-device";
        var manager = new GeminiSessionManager(
            logger.Object, conversation.Object, tools.Object, executor.Object, semaphore.Object);

        var result = useWithPath
            ? await manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(
                new GeminiGenerateRequest(), "Repeat until limit".AsMemory(), settings,
                CancellationToken.None, progress.Object)
            : await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                new GeminiGenerateRequest(), "Repeat until limit".AsMemory(), settings,
                CancellationToken.None, progress.Object);

        modelCalls.Should().Be(expectedSteps);
        toolCalls.Should().Be(expectedSteps);
        progressLimits.Should().NotBeEmpty().And.OnlyContain(limit => limit == expectedSteps);
        settings.MaxSteps.Should().Be(requestedSteps);
        var terminal = reports.Last();
        terminal.CurrentAction.Should().Be(Constants.ExecutionStatus.ERROR);
        terminal.Percentage.Should().Be(100);
        terminal.CurrentStep.Should().Be(expectedSteps);
        terminal.MaxSteps.Should().Be(expectedSteps);
        terminal.Metadata.Should().Contain("DeviceId", "test-device");
        reports.Count(report => report.CurrentAction == Constants.ExecutionStatus.ERROR).Should().Be(1);
        var failure = result.StatusList.Should().ContainSingle(status => !status.IsSuccess).Subject;
        failure.ErrorMessage.Should().Be(string.Format(
            Constants.Messages.FailureMessages.MAX_STEPS_REACHED_FORMAT, expectedSteps));
    }
}