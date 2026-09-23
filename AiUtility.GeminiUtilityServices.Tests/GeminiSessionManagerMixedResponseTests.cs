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

public sealed class GeminiSessionManagerMixedResponseTests
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
    public async Task MixedResponse_ExecutesToolBeforeReturningFinalText(
        bool useWithPath, bool textFirst, bool sequential)
    {
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
            conversation.Setup(x => x.WithSendMessageAsync(
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
    }

    private static GeminiResponse Response(IEnumerable<GeminiPart> parts) => new()
    {
        Candidates = new List<GeminiCandidate>
        {
            new() { Content = new GeminiMessage { Role = "model", Parts = parts.ToList() } }
        }
    };
}