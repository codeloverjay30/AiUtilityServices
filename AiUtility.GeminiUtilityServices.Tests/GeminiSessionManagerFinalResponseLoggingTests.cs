using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using CommonModels;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using ThreadLevelLockingUtilityServices;

namespace AiUtility.GeminiUtilityServices.Tests;

public sealed class GeminiSessionManagerFinalResponseLoggingTests
{
    [Theory]
    [InlineData(false, 499)]
    [InlineData(true, 499)]
    [InlineData(false, 500)]
    [InlineData(true, 500)]
    [InlineData(false, 501)]
    [InlineData(true, 501)]
    public async Task FinalResponse_TruncatesOnlyLogMessage(bool useWithPath, int length)
    {
        var logger = new Mock<ILoggerFactoryBaseUtilityService>(MockBehavior.Strict);
        var sink = new Mock<ILogger>();
        sink.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        logger.SetupGet(x => x.Logger).Returns(sink.Object);
        var conversation = new Mock<IGeminiConversationManager>(MockBehavior.Strict);
        conversation.SetupGet(x => x.LastTotalTokens).Returns(0);
        var tools = new Mock<IGeminiToolService>(MockBehavior.Strict);
        tools.Setup(x => x.SyncToolsToRequest(It.IsAny<GeminiGenerateRequest>()));
        var executor = new Mock<IGeminiToolExecutor>(MockBehavior.Strict);
        var semaphore = new Mock<ISemaphoreSlimService>(MockBehavior.Strict);
        semaphore.Setup(x => x.LockWithTimeoutValueAsync(
                It.IsAny<CancellationToken>(), It.IsAny<TimeSpan>(), false))
            .ReturnsAsync(Mock.Of<IDisposable>());

        var answer = new string('x', length);
        var parts = new List<GeminiPart> { new() { Text = answer } };
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
        status.Result.Should().Be(answer);
        reports.Should().NotBeEmpty();
        reports.Last().CurrentAction.Should().Be(Constants.ExecutionStatus.AI_COMPLETES_TASK);
        reports.Last().Percentage.Should().Be(100);
        reports.Last().CurrentStep.Should().Be(1);
        var logs = sink.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => new
            {
                Level = (LogLevel)invocation.Arguments[0],
                Fields = ((IEnumerable<KeyValuePair<string, object?>>)invocation.Arguments[2])
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            })
            .Where(log => log.Fields.TryGetValue("{OriginalFormat}", out var format)
                && Equals(format, "Gemini final response: {Message}"))
            .ToList();
        var log = logs.Should().ContainSingle().Subject;
        log.Level.Should().Be(LogLevel.Information);
        log.Fields["Message"].Should().Be(length > 500 ? new string('x', 500) + "..." : answer);
    }
}