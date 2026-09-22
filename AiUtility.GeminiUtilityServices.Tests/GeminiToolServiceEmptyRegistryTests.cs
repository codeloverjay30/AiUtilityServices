using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using AiUtility.ToolKits.Abstractions;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using static AiUtility.GeminiUtilityServices.Models.GeminiGenerateRequest;

namespace AiUtility.GeminiUtilityServices.Tests
{
    public class GeminiToolServiceEmptyRegistryTests
    {
        [Fact]
        public void SyncToolsToRequest_WhenRegistryIsEmpty_ShouldClearExistingTools()
        {
            // Arrange
            var registry = new Mock<IGeminiToolRegistry>(MockBehavior.Strict);
            registry
                .Setup(mock => mock.GetAllTools())
                .Returns([]);

            var converter = new Mock<IAiToolConverter<object>>(MockBehavior.Strict);

            var loggerFactory = new Mock<ILoggerFactoryBaseUtilityService>(
                MockBehavior.Strict);

            loggerFactory
                .Setup(factory => factory.Logger)
                .Returns(new Mock<ILogger>().Object);

            var request = new GeminiGenerateRequest
            {
                Tools = new List<GeminiToolDeclarationWrapper>
                {
                    new GeminiToolDeclarationWrapper()
                }
            };

            var sut = new GeminiToolService(
                registry.Object,
                converter.Object,
                loggerFactory.Object,
                false);

            // Act
            sut.SyncToolsToRequest(request);

            // Assert
            request.Tools.Should().NotBeNull().And.BeEmpty();

            registry.Verify(
                mock => mock.GetAllTools(),
                Times.Once);

            converter.Verify(
                mock => mock.ToToolDeclaration(
                    It.IsAny<AiUtility.ToolKits.Abstractions.ToolMetadataBase>()),
                Times.Never);
        }
    }
}