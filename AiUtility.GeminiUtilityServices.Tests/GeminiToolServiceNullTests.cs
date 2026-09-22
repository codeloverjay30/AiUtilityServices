using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Services;
using AiUtility.ToolKits.Abstractions;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AiUtility.GeminiUtilityServices.Tests
{
    public class GeminiToolServiceNullTests
    {
        [Fact]
        public void SyncToolsToRequest_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var registry = new Mock<IGeminiToolRegistry>(MockBehavior.Strict);
            var converter = new Mock<IAiToolConverter<object>>(MockBehavior.Strict);

            var loggerFactory = new Mock<ILoggerFactoryBaseUtilityService>(
                MockBehavior.Strict);

            loggerFactory
                .Setup(factory => factory.Logger)
                .Returns(new Mock<ILogger>().Object);

            var sut = new GeminiToolService(
                registry.Object,
                converter.Object,
                loggerFactory.Object,
                false);

            // Act
            Action act = () => sut.SyncToolsToRequest(null!);

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .WithMessage("*request*")
                .Which.ParamName.Should().Be("request");

            registry.Verify(
                mock => mock.GetAllTools(),
                Times.Never);
        }
    }
}