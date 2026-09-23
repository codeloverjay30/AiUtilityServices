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

public sealed class GeminiSessionManagerSequentialCancellationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SequentialTools_StopBeforeNextToolWhenCancelled(bool useWithPath, bool cancelAfterFirst)
    {
        using var cancellation = new CancellationTokenSource();
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
        var executedTools = new List<string>();
        var executor = new Mock<IGeminiToolExecutor>(MockBehavior.Strict);
        executor.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IDictionary<string, object>, CancellationToken>((name, args, token) =>
            {
                executedTools.Add(name);
                if (name == "First" && cancelAfterFirst)
                    cancellation.Cancel();
            })
            .ReturnsAsync(new { status = "ok" });

        var modelCalls = 0;
        GeminiResponse Respond(GeminiGenerateRequest request)
        {
            modelCalls++;
            return modelCalls == 1
                ? Response(new[]
                {
                    new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "First" } },
                    new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "Second" } }
                })
                : Response(new[] { new GeminiPart { Text = "Finished" } });
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
        var settings = new AiExecutionSettings { MaxSteps = 2, ForceSequentialToolExecution = true };
        StatusJsonModels? result = null;
        Action execute = () =>
        {
            result = (useWithPath
                ? manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(
                    new GeminiGenerateRequest(), "Run tools".AsMemory(), settings, cancellation.Token)
                : manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                    new GeminiGenerateRequest(), "Run tools".AsMemory(), settings, cancellation.Token))
                .GetAwaiter().GetResult();
        };

        if (cancelAfterFirst)
        {
            execute.Should().Throw<OperationCanceledException>()
                .WithMessage(new OperationCanceledException().Message)
                .Which.CancellationToken.Should().Be(cancellation.Token);
            executedTools.Should().Equal("First");
            modelCalls.Should().Be(1);
        }
        else
        {
            execute.Should().NotThrow();
            executedTools.Should().Equal("First", "Second");
            modelCalls.Should().Be(2);
            result.Should().NotBeNull();
            result!.StatusList.Should().HaveCount(3).And.OnlyContain(status => status.IsSuccess);
            result.StatusList.Last().Result.Should().Be("Finished");
        }
    }

    private static GeminiResponse Response(IEnumerable<GeminiPart> parts) => new()
    {
        Candidates = new List<GeminiCandidate>
        {
            new() { Content = new GeminiMessage { Role = "model", Parts = parts.ToList() } }
        }
    };
}