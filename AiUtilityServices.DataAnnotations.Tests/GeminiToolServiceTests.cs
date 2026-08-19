using AiUtility.AiBaseUtilityServices.Services;
using LoggerFactoryUtilityServices;
using Moq;

namespace AiUtility.Configurations.Tests
{
    public class GeminiApiClientTests
    {
        private readonly string _configPath = @"D:\workspace\utility packages\AiModels\AiUtilityServices\AiUtilityServices.Tests\secure.config.json5";

        // 修正：移除 "TestApp" 參數
        private readonly Mock<ILoggerFactoryBaseUtilityService> _mockLoggerFactory = new Mock<ILoggerFactoryBaseUtilityService>();

        public ApiKeyConfig ApiKeyConfiguration { get; set; }
    }
}
