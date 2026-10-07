using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using CommonModels;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using TaskUtilityServices;
using ThreadLevelLockingUtilityServices;
using Xunit;

namespace AiUtility.GeminiUtilityServices.Tests
{
    public class GeminiSessionManagerFunctionResponseRoleTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ToolResponse_ShouldBeSentWithUserRole_OnSecondModelRequest(bool useWithPath)
        {
            var loggerFactory = new Mock<ILoggerFactoryBaseUtilityService>();
            loggerFactory.Setup(x => x.Logger).Returns(Mock.Of<ILogger>());

            var conversation = new Mock<IGeminiConversationManager>(MockBehavior.Strict);
            conversation.SetupGet(x => x.LastTotalTokens).Returns(0);
            var toolService = new Mock<IGeminiToolService>();
            var executor = new Mock<IGeminiToolExecutor>(MockBehavior.Strict);
            var semaphore = new Mock<ISemaphoreSlimService>();
            semaphore.Setup(x => x.LockWithTimeoutValueAsync(
                    It.IsAny<CancellationToken>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
                .ReturnsAsync(Mock.Of<IDisposable>());

            var firstResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart
                                {
                                    FunctionCall = new GeminiFunctionCall { Name = "RoleProbe" }
                                }
                            }
                        }
                    }
                }
            };
            var finalResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart> { new GeminiPart { Text = "Finished" } }
                        }
                    }
                }
            };

            executor.Setup(x => x.ExecuteAsync(
                    "RoleProbe",
                    It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new { status = "ok" });

            int sendCount = 0;
            string? observedRole = null;
            bool observedFunctionResponse = false;

            GeminiResponse Respond(GeminiGenerateRequest current)
            {
                sendCount++;
                if (sendCount == 1)
                {
                    return firstResponse;
                }

                if (sendCount == 2)
                {
                    var responseMessage = current.Contents?
                        .LastOrDefault(message => message.Parts?.Any(part => part.FunctionResponse != null) == true);
                    observedRole = responseMessage?.Role;
                    observedFunctionResponse = responseMessage?.Parts?.Any(
                        part => part.FunctionResponse != null) == true;
                    return finalResponse;
                }

                throw new InvalidOperationException("Unexpected third model request.");
            }

            if (useWithPath)
            {
                conversation.Setup(x => x.SendMessageAsync(
                        It.IsAny<GeminiGenerateRequest>(),
                        It.IsAny<ReadOnlyMemory<char>>(),
                        It.IsAny<AiExecutionSettings>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((GeminiGenerateRequest current,
                        ReadOnlyMemory<char> _, AiExecutionSettings _, CancellationToken _) => Respond(current));
            }
            else
            {
                conversation.Setup(x => x.SendMessageAsync(
                        It.IsAny<GeminiGenerateRequest>(),
                        It.IsAny<ReadOnlyMemory<char>>(),
                        It.IsAny<AiExecutionSettings>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((GeminiGenerateRequest current,
                        ReadOnlyMemory<char> _, AiExecutionSettings _, CancellationToken _) => Respond(current));
            }

            var manager = new GeminiSessionManager(
                loggerFactory.Object, conversation.Object, toolService.Object,
                executor.Object, semaphore.Object);
            var settings = new AiExecutionSettings { ForceSequentialToolExecution = true };
            var request = new GeminiGenerateRequest();

            if (useWithPath)
            {
                await manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(
                    request, "Check function response role".AsMemory(), settings);
            }
            else
            {
                await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                    request, "Check function response role".AsMemory(), settings);
            }

            sendCount.Should().Be(2, "the model must receive the tool result in a second request");
            observedFunctionResponse.Should().BeTrue("the second request must contain a function response");
            observedRole.Should().Be("user", "Gemini function responses must be sent with the user role");
        }
    }
}
