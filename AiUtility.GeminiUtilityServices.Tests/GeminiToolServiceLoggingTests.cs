using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Models;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using AiUtility.ToolKits.Abstractions;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;

namespace AiUtility.GeminiUtilityServices.Tests
{
    public class GeminiToolServiceLoggingTests
    {
        private static void LoggingTestTool()
        {
        }

        [Fact]
        public void SyncToolsToRequest_WhenRegistryIsEmpty_ShouldLogCountAndWarning()
        {
            // Arrange
            var registry = new Mock<IGeminiToolRegistry>(MockBehavior.Strict);
            registry.Setup(mock => mock.GetAllTools()).Returns([]);

            var converter = new Mock<IAiToolConverter<object>>(MockBehavior.Strict);
            var logger = new Mock<ILogger>(MockBehavior.Loose);
            logger
                .Setup(mock => mock.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);

            var loggerFactory =
                new Mock<ILoggerFactoryBaseUtilityService>(MockBehavior.Strict);

            loggerFactory
                .Setup(factory => factory.Logger)
                .Returns(logger.Object);

            var sut = new GeminiToolService(
                registry.Object,
                converter.Object,
                loggerFactory.Object,
                false);

            var request = new GeminiGenerateRequest();

            // Act
            sut.SyncToolsToRequest(request);

            // Assert
            request.Tools.Should().NotBeNull().And.BeEmpty();

            VerifyLog(
                logger,
                LogLevel.Information,
                "Synchronizing Gemini tools. ToolCount=0",
                Times.Once());

            VerifyLog(
                logger,
                LogLevel.Warning,
                "No Gemini tools are registered. Gemini cannot perform tool execution.",
                Times.Once());
        }

        [Fact]
        public void SyncToolsToRequest_WhenRegistryHasTools_ShouldLogCountWithoutWarning()
        {
            // Arrange
            var method = typeof(GeminiToolServiceLoggingTests)
                .GetMethod(
                    nameof(LoggingTestTool),
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static)!;

            var metadata = new GeminiToolMetadata(
                method.Name,
                method,
                method.GetParameters(),
                (_, _) => null,
                null,
                method.GetCustomAttributes(inherit: false)
                    .Cast<Attribute>());
            var registry = new Mock<IGeminiToolRegistry>(MockBehavior.Strict);
            registry
                .Setup(mock => mock.GetAllTools())
                .Returns([metadata]);

            var declaration = new GeminiToolDeclaration();
            var converter = new Mock<IAiToolConverter<object>>(MockBehavior.Strict);
            converter
                .Setup(mock => mock.ToToolDeclaration(metadata))
                .Returns(declaration);

            var logger = new Mock<ILogger>(MockBehavior.Loose);
            logger
                .Setup(mock => mock.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);
            var loggerFactory =
                new Mock<ILoggerFactoryBaseUtilityService>(MockBehavior.Strict);

            loggerFactory
                .Setup(factory => factory.Logger)
                .Returns(logger.Object);

            var sut = new GeminiToolService(
                registry.Object,
                converter.Object,
                loggerFactory.Object,
                false);

            var request = new GeminiGenerateRequest();

            // Act
            sut.SyncToolsToRequest(request);

            // Assert
            request.Tools.Should().ContainSingle();

            VerifyLog(
                logger,
                LogLevel.Information,
                "Synchronizing Gemini tools. ToolCount=1",
                Times.Once());

            VerifyLog(
                logger,
                LogLevel.Warning,
                "No Gemini tools are registered. Gemini cannot perform tool execution.",
                Times.Never());
        }

        private static void VerifyLog(
            Mock<ILogger> logger,
            LogLevel expectedLevel,
            string expectedMessage,
            Times times)
        {
            logger.Verify(
                mock => mock.Log(
                    It.Is<LogLevel>(level => level == expectedLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString() == expectedMessage),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }
    }
}