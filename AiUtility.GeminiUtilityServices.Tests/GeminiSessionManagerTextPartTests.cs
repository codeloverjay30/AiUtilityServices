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

public sealed class GeminiSessionManagerTextPartTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public async Task TextResponse_ReturnsFirstNonEmptyTextPart(bool useWithPath, int leadingParts)
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

        var parts = Enumerable.Range(0, leadingParts)
            .Select(index => new GeminiPart { Text = index == 0 ? null : string.Empty })
            .ToList();
        parts.Add(new GeminiPart { Text = "The final answer." });
        parts.Add(new GeminiPart { Text = "Later text." });
        var response = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>
            {
                new() { Content = new GeminiMessage { Role = "model", Parts = parts } }
            }
        };
        var modelCalls = 0;
        if (useWithPath)
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => modelCalls++)
                .ReturnsAsync(response);
        else
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => modelCalls++)
                .ReturnsAsync(response);

        var reports = new List<WorkflowProgress>();
        var progress = new Mock<IProgress<WorkflowProgress>>();
        progress.Setup(x => x.Report(It.IsAny<WorkflowProgress>()))
            .Callback<WorkflowProgress>(reports.Add);
        var settings = new AiExecutionSettings { MaxSteps = 2 };
        var manager = new GeminiSessionManager(
            logger.Object, conversation.Object, tools.Object, executor.Object, semaphore.Object);
        var result = useWithPath
            ? await manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(
                new GeminiGenerateRequest(), "Answer".AsMemory(), settings,
                CancellationToken.None, progress.Object)
            : await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                new GeminiGenerateRequest(), "Answer".AsMemory(), settings,
                CancellationToken.None, progress.Object);

        modelCalls.Should().Be(1);
        executor.Invocations.Should().BeEmpty();
        var status = result.StatusList.Should().ContainSingle().Subject;
        status.IsSuccess.Should().BeTrue();
        status.Result.Should().Be("The final answer.");
        reports.Should().NotBeEmpty();
        reports.Last().CurrentAction.Should().Be(Constants.ExecutionStatus.AI_COMPLETES_TASK);
        reports.Last().Percentage.Should().Be(100);
        reports.Last().CurrentStep.Should().Be(1);
    }
}