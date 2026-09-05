using AiUtility.AiBaseUtilityServices.Services;
using AiUtility.GeminiUtilityServices.Configs;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AiUtility.Configurations.Tests
{
    [TestFixture]
    public class GeminiApiClientTests
    {
        private static readonly JsonSerializerOptions _options = AiUtility.Common.Options.JsonOptions.DefaultOptions;

        private const string TestApiKey = "gen-lang-client-0455493629s";

        private Mock<ILoggerFactoryBaseUtilityService> _mockLoggerFactory;
        private Mock<ILogger<GeminiApiClient>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<GeminiApiClient>>();
            _mockLoggerFactory = new Mock<ILoggerFactoryBaseUtilityService>();

            _mockLoggerFactory
                .Setup(x => x.LoggerFactory.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);


        }

        [Test]
        public async Task GenerateContentAsync_TextAndImage_SendsCorrectJson()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var expectedResponseStr = """
            {
              "candidates": [
                {
                  "content": {
                    "role": "user",
                    "parts": [
                      {
                        "text": "Response from AI",
                        "inline_data": null,
                        "function_call": null,
                        "function_response": null
                      }
                    ]
                  }
                }
              ],
              "FunctionCall": null,
              "usageMetadata": null
            }
            """;

            var expectedResponse = JsonSerializer.Deserialize<GeminiResponse>(expectedResponseStr , _options);
            var expectedResult = JsonSerializer.Serialize<GeminiResponse>(expectedResponse , _options);

            var mockResult = 
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync" ,
                  ItExpr.IsAny<HttpRequestMessage>() ,
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK ,
                   Content = new StringContent(
                       expectedResponseStr ,
                       Encoding.UTF8 ,
                       CommonConstants.MimeTypes.MimeTypeConstants.APPLICATION_JSON // "application/json"
                   ),
               });

            var httpClient = new HttpClient(handlerMock.Object);
  
            var client = new GeminiApiClient(_mockLoggerFactory.Object , toLogWhenSuccess: false)
            {
                HttpClient = httpClient,
                ApiKey = TestApiKey,
                ApiOptions = new GeminiApiOptions
                {
                    Model = "gemini-test-model",
                },
            };

            var request = new GeminiGenerateRequest();
          
            byte [ ] fakeImage = { 0x01 , 0x02 , 0x03 };
            string expectedBase64 = Convert.ToBase64String(fakeImage);

            request.AddUserMessage("Hello Gemini" , fakeImage);
            // Act
            var response = await client.GenerateContentAsync(request);
            var result = JsonSerializer.Serialize<GeminiResponse>(response,_options);
            // Assert (使用 NUnit 語法)
            TestContext.WriteLine(response);
            TestContext.WriteLine(result);
            TestContext.WriteLine(expectedResponse);

            Assert.That(result , Is.EqualTo(expectedResult));

            handlerMock.Protected().Verify(
               "SendAsync" ,
               Times.Exactly(1) ,
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Post &&
                  req.RequestUri.Query.Contains($"key={TestApiKey}")) ,
               ItExpr.IsAny<CancellationToken>()
            );
        }

        [Test]
        public async Task GenerateContentAsync_ApiError_ThrowsException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync" ,
                  ItExpr.IsAny<HttpRequestMessage>() ,
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.BadRequest ,
                   Content = new StringContent("Invalid Request")
               });

            var client = new GeminiApiClient(_mockLoggerFactory.Object , false)
            {
                HttpClient = new HttpClient(handlerMock.Object),
                ApiKey = TestApiKey,
                ApiOptions = new GeminiApiOptions
                {
                    Model = "gemini-test-model",
                },
            };

            var request = new GeminiGenerateRequest();

            request.AddUserMessage("Error Test".AsMemory());

            // Act & Assert (使用 NUnit 語法)
            Assert.ThrowsAsync<HttpRequestException>(async () =>
                await client.GenerateContentAsync(request));
        }
    }
}
