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

public sealed class GeminiSessionManagerCompletionLoggingTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Workflow_LogsCompletionOnceWithLatestTokens(bool useWithPath, bool fail)
    {
        var logger = new Mock<ILoggerFactoryBaseUtilityService>(MockBehavior.Strict);
        var sink = new Mock<ILogger>();
        sink.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        logger.SetupGet(x => x.Logger).Returns(sink.Object);
        var conversation = new Mock<IGeminiConversationManager>(MockBehavior.Strict);
        var tokens = 0;
        conversation.SetupGet(x => x.LastTotalTokens).Returns(() => tokens);
        var tools = new Mock<IGeminiToolService>(MockBehavior.Strict);
        tools.Setup(x => x.SyncToolsToRequest(It.IsAny<GeminiGenerateRequest>()));
        var executor = new Mock<IGeminiToolExecutor>(MockBehavior.Strict);
        var semaphore = new Mock<ISemaphoreSlimService>(MockBehavior.Strict);
        semaphore.Setup(x => x.LockWithTimeoutValueAsync(
                It.IsAny<CancellationToken>(), It.IsAny<TimeSpan>(), false))
            .ReturnsAsync(Mock.Of<IDisposable>());

        var calls = 0;
        Task<GeminiResponse> Respond()
        {
            calls++;
            tokens = 42;
            return fail
                ? Task.FromException<GeminiResponse>(new InvalidOperationException("Model unavailable"))
                : Task.FromResult(new GeminiResponse
                {
                    Candidates = new List<GeminiCandidate>
                    {
                        new() { Content = new GeminiMessage
                        {
                            Role = "model",
                            Parts = new List<GeminiPart> { new() { Text = "Done" } }
                        }}
                    }
                });
        }
        if (useWithPath)
        {
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Returns(Respond);
        }
        else
        {
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .Returns(Respond);
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
        status.IsSuccess.Should().Be(!fail);
        if (fail)
            status.ErrorMessage.Should().Be("Model unavailable");
        else
            status.Result.Should().Be("Done");

        var completionLogs = sink.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => new
            {
                Level = (LogLevel)invocation.Arguments[0],
                Fields = ((IEnumerable<KeyValuePair<string, object?>>)invocation.Arguments[2])
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            })
            .Where(log => log.Fields.TryGetValue("{OriginalFormat}", out var format)
                && Equals(format, "Finish AI Workflow for task: {TaskName}, Current Memory Tokens: {Tokens}"))
            .ToList();
        var log = completionLogs.Should().ContainSingle().Subject;
        log.Level.Should().Be(LogLevel.Information);
        log.Fields.Should().Contain("Tokens", 42);
    }
}