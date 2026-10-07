using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using AiUtility.ToolKits.Abstractions;
using CommonModels;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using ThreadLevelLockingUtilityServices;
using Xunit;

namespace AiUtility.GeminiUtilityServices.Tests
{
    public class GeminiFunctionResponseRoleTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ToolResponse_ShouldUseUserRole_OnSecondModelRequest(
            bool useWithRequestPath)
        {
            // Arrange
            var loggerFactory = new Mock<ILoggerFactoryBaseUtilityService>();
            loggerFactory
                .Setup(x => x.Logger)
                .Returns(new Mock<ILogger>().Object);

            var conversationManager = new Mock<IGeminiConversationManager>();
            var toolService = new Mock<IGeminiToolService>();
            var toolExecutor = new Mock<IGeminiToolExecutor>(MockBehavior.Strict);
            var semaphoreService = new Mock<ISemaphoreSlimService>();

            semaphoreService
                .Setup(x => x.LockWithTimeoutValueAsync(
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new Mock<IDisposable>().Object);

            toolExecutor
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, object>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new { status = "success" });

            var toolCallResponse = new GeminiResponse
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
                                    FunctionCall = new GeminiFunctionCall
                                    {
                                        Name = "TestTool"
                                    }
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
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { Text = "Finished" }
                            }
                        }
                    }
                }
            };

            var modelCallCount = 0;
            string? secondRequestToolResponseRole = null;

            Task<GeminiResponse> RespondToModelRequest(
                GeminiGenerateRequest modelRequest)
            {
                modelCallCount++;

                if (modelCallCount == 1)
                {
                    return Task.FromResult(toolCallResponse);
                }

                if (modelCallCount == 2)
                {
                    // Capture the role at invocation time. The mutable request
                    // path may append further messages after this invocation.
                    secondRequestToolResponseRole = modelRequest.Contents?
                        .LastOrDefault(message =>
                            message.Parts?.Any(part =>
                                part.FunctionResponse != null) == true)?
                        .Role;

                    return Task.FromResult(finalResponse);
                }

                throw new InvalidOperationException(
                    "Unexpected additional model request.");
            }

            if (useWithRequestPath)
            {
                conversationManager
                    .Setup(x => x.SendMessageAsync(
                        It.IsAny<GeminiGenerateRequest>(),
                        It.IsAny<ReadOnlyMemory<char>>(),
                        It.IsAny<AiExecutionSettings>(),
                        It.IsAny<CancellationToken>()))
                    .Returns((
                        GeminiGenerateRequest modelRequest,
                        ReadOnlyMemory<char> _,
                        AiExecutionSettings __,
                        CancellationToken ___) =>
                        RespondToModelRequest(modelRequest));
            }
            else
            {
                conversationManager
                    .Setup(x => x.SendMessageAsync(
                        It.IsAny<GeminiGenerateRequest>(),
                        It.IsAny<ReadOnlyMemory<char>>(),
                        It.IsAny<AiExecutionSettings>(),
                        It.IsAny<CancellationToken>()))
                    .Returns((
                        GeminiGenerateRequest modelRequest,
                        ReadOnlyMemory<char> _,
                        AiExecutionSettings __,
                        CancellationToken ___) =>
                        RespondToModelRequest(modelRequest));
            }

            var manager = new GeminiSessionManager(
                loggerFactory.Object,
                conversationManager.Object,
                toolService.Object,
                toolExecutor.Object,
                semaphoreService.Object);

            var request = new GeminiGenerateRequest();
            var settings = new AiExecutionSettings
            {
                ForceSequentialToolExecution = true
            };

            // Act
            if (useWithRequestPath)
            {
                await manager.WithExecuteWithToolSupportAsync<WorkflowProgress>(
                    request,
                    "Role regression test".AsMemory(),
                    settings);
            }
            else
            {
                await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                    request,
                    "Role regression test".AsMemory(),
                    settings);
            }

            // Assert
            modelCallCount.Should().Be(
                2,
                "the tool response must be sent in a second model request");

            secondRequestToolResponseRole.Should().Be(
                "user",
                "Gemini function responses must be sent with the user role");
        }
    }
}