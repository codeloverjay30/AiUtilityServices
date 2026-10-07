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

public sealed class GeminiSessionManagerResponseLoggingTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task Responses_LogStructureForEachModelRound(
        bool useWithPath, bool textFirst, bool sequential)
    {
        var logger = new Mock<ILoggerFactoryBaseUtilityService>(MockBehavior.Strict);
        var sink = new Mock<ILogger>();
        sink.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        logger.SetupGet(x => x.Logger).Returns(sink.Object);
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
                "Lookup", It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .Callback(() => toolCalls++)
            .ReturnsAsync(new { status = "ok" });

        var text = new GeminiPart { Text = "I will look it up." };
        var call = new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "Lookup" } };
        var mixed = Response(textFirst ? new[] { text, call } : new[] { call, text });
        var final = Response(new[] { new GeminiPart { Text = "Finished" } });
        var modelCalls = 0;
        var toolCallsAtSecondRequest = 0;
        var responseRoles = Array.Empty<string>();
        var responseNames = Array.Empty<string>();
        GeminiResponse Respond(GeminiGenerateRequest request)
        {
            modelCalls++;
            if (modelCalls == 1)
                return mixed;
            if (modelCalls == 2)
            {
                // Capture values during the call: later request mutation cannot affect assertions.
                toolCallsAtSecondRequest = toolCalls;
                responseRoles = request.Contents
                    .Where(message => message.Parts.Any(part => part.FunctionResponse != null))
                    .Select(message => message.Role).ToArray();
                responseNames = request.Contents.SelectMany(message => message.Parts)
                    .Where(part => part.FunctionResponse != null)
                    .Select(part => part.FunctionResponse!.Name).ToArray();
            }
            return final;
        }

        if (useWithPath)
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GeminiGenerateRequest request, ReadOnlyMemory<char> task,
                    AiExecutionSettings settings, CancellationToken ct) => Respond(request));
        else
            conversation.Setup(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>(), It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GeminiGenerateRequest request, ReadOnlyMemory<char> task,
                    AiExecutionSettings settings, CancellationToken ct) => Respond(request));

        var manager = new GeminiSessionManager(
            logger.Object, conversation.Object, tools.Object, executor.Object, semaphore.Object);
        var settings = new AiExecutionSettings { MaxSteps = 2, ForceSequentialToolExecution = sequential };
        var result = useWithPath
            ? await manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(
                new GeminiGenerateRequest(), "Look up value".AsMemory(), settings)
            : await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                new GeminiGenerateRequest(), "Look up value".AsMemory(), settings);

        modelCalls.Should().Be(2);
        toolCalls.Should().Be(1);
        toolCallsAtSecondRequest.Should().Be(1);
        responseRoles.Should().Equal("user");
        responseNames.Should().Equal("Lookup");
        result.StatusList.Should().HaveCount(2).And.OnlyContain(status => status.IsSuccess);
        result.StatusList.Last().Result.Should().Be("Finished");
        var logs = sink.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => new
            {
                Level = (LogLevel)invocation.Arguments[0],
                Fields = ((IEnumerable<KeyValuePair<string, object?>>)invocation.Arguments[2])
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            })
            .Where(log => log.Fields.ContainsKey("PartCount"))
            .ToList();
        logs.Should().HaveCount(2);
        logs.Should().OnlyContain(log => log.Level == LogLevel.Information);
        logs[0].Fields.Should().Contain("PartCount", 2)
            .And.Contain("FunctionCallCount", 1).And.Contain("HasText", true);
        logs[1].Fields.Should().Contain("PartCount", 1)
            .And.Contain("FunctionCallCount", 0).And.Contain("HasText", true);
    }

    private static GeminiResponse Response(IEnumerable<GeminiPart> parts) => new()
    {
        Candidates = new List<GeminiCandidate>
        {
            new() { Content = new GeminiMessage { Role = "model", Parts = parts.ToList() } }
        }
    };
}