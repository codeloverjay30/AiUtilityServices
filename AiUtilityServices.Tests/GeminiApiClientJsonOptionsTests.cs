using AiUtility.GeminiUtilityServices.Configs;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AiUtility.Configurations.Tests;

[TestFixture]
public class GeminiApiClientJsonOptionsTests
{
    private const string ResponseJson = """{"candidates":[]}""";

    [Test]
    public async Task GenerateContentAsync_WhenOptionsAreProvided_UsesProvidedOptions()
    {
        var customOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        string requestJson = await SendRequestAsync(customOptions);

        using JsonDocument document = JsonDocument.Parse(requestJson);

        // 驗證呼叫端提供的命名規則確實作用於送出的 request。
        document.RootElement.TryGetProperty(
                "generation_config",
                out _)
            .Should()
            .BeTrue();

        requestJson.Should().NotContain(Environment.NewLine);
    }

    [Test]
    public async Task GenerateContentAsync_WhenOptionsAreNotProvided_UsesDefaultOptions()
    {
        string requestJson = await SendRequestAsync(jsonOptions: null);

        using JsonDocument document = JsonDocument.Parse(requestJson);

        // 專案的 DefaultOptions 設定 WriteIndented = true。
        requestJson.Should().Contain(Environment.NewLine);
    }

    private static async Task<string> SendRequestAsync(
        JsonSerializerOptions? jsonOptions)
    {
        var loggerMock = new Mock<ILogger>(MockBehavior.Loose);

        var loggerFactoryMock =
            new Mock<ILoggerFactoryBaseUtilityService>(
                MockBehavior.Strict);

        loggerFactoryMock
            .SetupGet(factory => factory.Logger)
            .Returns(loggerMock.Object);

        loggerFactoryMock
            .SetupGet(factory => factory.IsLoggerCreated)
            .Returns(true);

        string? capturedRequestJson = null;

        var handlerMock = new Mock<HttpMessageHandler>(
            MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(
                async (message, _) =>
                {
                    capturedRequestJson =
                        await message.Content!
                            .ReadAsStringAsync();

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            ResponseJson,
                            Encoding.UTF8,
                            "application/json")
                    };
                });

        using var httpClient = new HttpClient(
            handlerMock.Object,
            disposeHandler: false);

        var client = new GeminiApiClient(
            loggerFactoryMock.Object,
            toLogWhenSuccess: false,
            jsonOptions: jsonOptions)
        {
            HttpClient = httpClient,
            ApiKey = "test-api-key",
            ApiOptions = new GeminiApiOptions
            {
                Model = "gemini-test-model"
            }
        };

        var request = new GeminiGenerateRequest();
        request.AddUserMessage("Test JSON options".AsMemory());

        await client.GenerateContentAsync(request);

        capturedRequestJson.Should().NotBeNullOrWhiteSpace();

        return capturedRequestJson!;
    }
}