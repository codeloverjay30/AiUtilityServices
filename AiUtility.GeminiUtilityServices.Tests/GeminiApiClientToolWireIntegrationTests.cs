using AiUtility.GeminiKits.Models;
using AiUtility.GeminiUtilityServices.Configs;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using static AiUtility.GeminiUtilityServices.Models.GeminiGenerateRequest;

namespace AiUtility.GeminiUtilityServices.Tests;

/// <summary>
/// Contains integration tests for the final Gemini tool HTTP wire contract.
/// </summary>
public sealed class GeminiApiClientToolWireIntegrationTests
{
    private const string TestApiKey =
        "integration-test-api-key";

    private readonly Mock<ILogger>
        _loggerMock;

    private readonly Mock<ILoggerFactoryBaseUtilityService>
        _loggerFactoryServiceMock;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GeminiApiClientToolWireIntegrationTests"/> class.
    /// </summary>
    public GeminiApiClientToolWireIntegrationTests()
    {
        _loggerMock =
            new Mock<ILogger>(
                MockBehavior.Loose);

        _loggerFactoryServiceMock =
            new Mock<ILoggerFactoryBaseUtilityService>(
                MockBehavior.Strict);

        _loggerFactoryServiceMock
            .SetupGet(
                service =>
                    service.Logger)
            .Returns(
                _loggerMock.Object);

        _loggerFactoryServiceMock
            .SetupGet(
                service =>
                    service.IsLoggerCreated)
            .Returns(
                true);
    }

    /// <summary>
    /// Verifies that a complex tool parameter is written to the actual HTTP
    /// request body as a Gemini object schema.
    /// </summary>
    [Fact]
    public async Task GenerateContentAsync_ComplexToolParameter_ShouldSendObjectSchemaInHttpBody()
    {
        // Arrange
        string? capturedJson =
            null;

        using var httpClient =
            CreateHttpClient(
                body =>
                    capturedJson = body);

        var client =
            CreateClient(
                httpClient);

        var request =
            CreateComplexObjectRequest();

        // Act
        var response =
            await client.GenerateContentAsync(
                request);

        // Assert
        response.Should()
            .NotBeNull();

        capturedJson.Should()
            .NotBeNullOrWhiteSpace();

        using var document =
            JsonDocument.Parse(
                capturedJson!);

        var target =
            GetParameterProperty(
                document.RootElement,
                "target");

        target.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "object");

        target.GetProperty(
                "type")
            .GetString()
            .Should()
            .NotBe(
                "other");

        var properties =
            target.GetProperty(
                "properties");

        properties.TryGetProperty(
                "x",
                out var x)
            .Should()
            .BeTrue();

        properties.TryGetProperty(
                "y",
                out var y)
            .Should()
            .BeTrue();

        x.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "integer");

        y.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "integer");
    }

    /// <summary>
    /// Verifies that a collection of complex tool parameters is written to
    /// the actual HTTP request body as an array containing object items.
    /// </summary>
    [Fact]
    public async Task GenerateContentAsync_ComplexCollectionParameter_ShouldSendArrayOfObjectsInHttpBody()
    {
        // Arrange
        string? capturedJson =
            null;

        using var httpClient =
            CreateHttpClient(
                body =>
                    capturedJson = body);

        var client =
            CreateClient(
                httpClient);

        var request =
            CreateComplexCollectionRequest();

        // Act
        var response =
            await client.GenerateContentAsync(
                request);

        // Assert
        response.Should()
            .NotBeNull();

        capturedJson.Should()
            .NotBeNullOrWhiteSpace();

        using var document =
            JsonDocument.Parse(
                capturedJson!);

        var targets =
            GetParameterProperty(
                document.RootElement,
                "targets");

        targets.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "array");

        var items =
            targets.GetProperty(
                "items");

        items.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "object");

        items.GetProperty(
                "type")
            .GetString()
            .Should()
            .NotBe(
                "other");

        var properties =
            items.GetProperty(
                "properties");

        properties.TryGetProperty(
                "x",
                out var x)
            .Should()
            .BeTrue();

        properties.TryGetProperty(
                "y",
                out var y)
            .Should()
            .BeTrue();

        x.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "integer");

        y.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "integer");
    }

    /// <summary>
    /// Verifies that required nested properties are preserved in the actual
    /// HTTP request body.
    /// </summary>
    [Fact]
    public async Task GenerateContentAsync_ComplexToolParameter_ShouldSendRequiredPropertiesInHttpBody()
    {
        // Arrange
        string? capturedJson =
            null;

        using var httpClient =
            CreateHttpClient(
                body =>
                    capturedJson = body);

        var client =
            CreateClient(
                httpClient);

        var request =
            CreateComplexObjectRequest();

        // Act
        var response =
            await client.GenerateContentAsync(
                request);

        // Assert
        response.Should()
            .NotBeNull();

        capturedJson.Should()
            .NotBeNullOrWhiteSpace();

        using var document =
            JsonDocument.Parse(
                capturedJson!);

        var target =
            GetParameterProperty(
                document.RootElement,
                "target");

        target.TryGetProperty(
                "required",
                out var required)
            .Should()
            .BeTrue();

        var requiredProperties =
            required
                .EnumerateArray()
                .Select(
                    static element =>
                        element.GetString())
                .Where(
                    static value =>
                        value is not null)
                .Cast<string>()
                .ToArray();

        requiredProperties.Should()
            .Contain(
                "x");

        requiredProperties.Should()
            .Contain(
                "y");
    }

    /// <summary>
    /// Verifies that the Gemini API client sends a POST request with the
    /// configured API key and JSON content type.
    /// </summary>
    [Fact]
    public async Task GenerateContentAsync_ToolRequest_ShouldUseExpectedHttpRequestContract()
    {
        // Arrange
        HttpMethod? capturedMethod =
            null;

        Uri? capturedUri =
            null;

        string? capturedMediaType =
            null;

        using var httpClient =
            CreateHttpClient(
                _ =>
                {
                },
                request =>
                {
                    capturedMethod =
                        request.Method;

                    capturedUri =
                        request.RequestUri;

                    capturedMediaType =
                        request.Content?
                            .Headers
                            .ContentType?
                            .MediaType;
                });

        var client =
            CreateClient(
                httpClient);

        var request =
            CreateComplexObjectRequest();

        // Act
        var response =
            await client.GenerateContentAsync(
                request);

        // Assert
        response.Should()
            .NotBeNull();

        capturedMethod.Should()
            .Be(
                HttpMethod.Post);

        capturedUri.Should()
            .NotBeNull();

        capturedUri!.Query.Should()
            .Contain(
                $"key={TestApiKey}");

        capturedMediaType.Should()
            .Be(
                "application/json");
    }

    /// <summary>
    /// Creates a Gemini API client configured to use the supplied HTTP
    /// client.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to intercept the outgoing request.
    /// </param>
    /// <returns>
    /// A configured Gemini API client.
    /// </returns>
    private GeminiApiClient CreateClient(
        HttpClient httpClient)
    {
        return new GeminiApiClient(
            _loggerFactoryServiceMock.Object,
            toLogWhenSuccess:
                false)
        {
            HttpClient =
                httpClient,

            ApiKey =
                TestApiKey,

            ApiOptions =
                new GeminiApiOptions
                {
                    Model =
                        "gemini-test-model",
                },
        };
    }

    /// <summary>
    /// Creates an HTTP client whose message handler captures the actual
    /// serialized request body.
    /// </summary>
    /// <param name="captureBody">
    /// The callback used to capture the serialized HTTP body.
    /// </param>
    /// <param name="captureRequest">
    /// An optional callback used to inspect the outgoing HTTP request.
    /// </param>
    /// <returns>
    /// An HTTP client backed by a mocked message handler.
    /// </returns>
    private static HttpClient CreateHttpClient(
        Action<string> captureBody,
        Action<HttpRequestMessage>? captureRequest =
            null)
    {
        var handlerMock =
            new Mock<HttpMessageHandler>(
                MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup(
                "Dispose",
                ItExpr.IsAny<bool>());

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback(
                (
                    HttpRequestMessage request,
                    CancellationToken _) =>
                {
                    captureRequest?.Invoke(
                        request);

                    var body =
                        request.Content is null
                            ? string.Empty
                            : request.Content
                                .ReadAsStringAsync()
                                .GetAwaiter()
                                .GetResult();

                    captureBody(
                        body);
                })
            .ReturnsAsync(
                CreateSuccessfulResponse());

        return new HttpClient(
            handlerMock.Object);
    }


    /// <summary>
    /// Creates a successful Gemini response used to complete the HTTP
    /// integration boundary without contacting the real Gemini API.
    /// </summary>
    /// <returns>
    /// A successful HTTP response containing a minimal valid Gemini response.
    /// </returns>
    private static HttpResponseMessage CreateSuccessfulResponse()
    {
        const string responseJson =
            """
            {
              "candidates": [
                {
                  "content": {
                    "role": "model",
                    "parts": [
                      {
                        "text": "ok"
                      }
                    ]
                  }
                }
              ]
            }
            """;

        return new HttpResponseMessage(
            HttpStatusCode.OK)
        {
            Content =
                new StringContent(
                    responseJson,
                    Encoding.UTF8,
                    "application/json"),
        };
    }

    /// <summary>
    /// Creates a Gemini request containing a tool declaration with a complex
    /// object parameter.
    /// </summary>
    /// <returns>
    /// A Gemini generation request containing the complex object tool.
    /// </returns>
    private static GeminiGenerateRequest CreateComplexObjectRequest()
    {
        var declaration =
            new GeminiToolDeclaration
            {
                Name =
                    "click",

                Description =
                    "Clicks a target.",

                Parameters =
                    new GeminiParameters
                    {
                        Type =
                            "object",

                        Properties =
                            new Dictionary<string, GeminiParameterProperty>
                            {
                                ["target"] =
                                    new GeminiParameterProperty
                                    {
                                        Type =
                                            "object",

                                        Properties =
                                            new Dictionary<string, GeminiParameterProperty>
                                            {
                                                ["x"] =
                                                    new GeminiParameterProperty
                                                    {
                                                        Type =
                                                            "integer",
                                                    },

                                                ["y"] =
                                                    new GeminiParameterProperty
                                                    {
                                                        Type =
                                                            "integer",
                                                    },
                                            },

                                        Required =
                                        [
                                            "x",
                                            "y",
                                        ],
                                    },
                            },

                        Required =
                        [
                            "target",
                        ],
                    },
            };

        return CreateRequest(
            declaration);
    }

    /// <summary>
    /// Creates a Gemini request containing a tool declaration with a
    /// collection of complex object parameters.
    /// </summary>
    /// <returns>
    /// A Gemini generation request containing the complex collection tool.
    /// </returns>
    private static GeminiGenerateRequest CreateComplexCollectionRequest()
    {
        var declaration =
            new GeminiToolDeclaration
            {
                Name =
                    "clickMany",

                Description =
                    "Clicks multiple targets.",

                Parameters =
                    new GeminiParameters
                    {
                        Type =
                            "object",

                        Properties =
                            new Dictionary<string, GeminiParameterProperty>
                            {
                                ["targets"] =
                                    new GeminiParameterProperty
                                    {
                                        Type =
                                            "array",

                                        Items =
                                            new GeminiParameterProperty
                                            {
                                                Type =
                                                    "object",

                                                Properties =
                                                    new Dictionary<string, GeminiParameterProperty>
                                                    {
                                                        ["x"] =
                                                            new GeminiParameterProperty
                                                            {
                                                                Type =
                                                                    "integer",
                                                            },

                                                        ["y"] =
                                                            new GeminiParameterProperty
                                                            {
                                                                Type =
                                                                    "integer",
                                                            },
                                                    },

                                                Required =
                                                [
                                                    "x",
                                                    "y",
                                                ],
                                            },
                                    },
                            },

                        Required =
                        [
                            "targets",
                        ],
                    },
            };

        return CreateRequest(
            declaration);
    }

    /// <summary>
    /// Creates a Gemini generation request containing the supplied tool
    /// declaration.
    /// </summary>
    /// <param name="declaration">
    /// The Gemini tool declaration.
    /// </param>
    /// <returns>
    /// A Gemini generation request containing the declaration.
    /// </returns>
    private static GeminiGenerateRequest CreateRequest(
        GeminiToolDeclaration declaration)
    {
        var request =
            new GeminiGenerateRequest();

        request.AddUserMessage(
            "Execute the tool.".AsMemory());

        request.Tools.Add(
            new GeminiToolDeclarationWrapper
            {
                FunctionDeclarations =
                [
                    declaration,
                ],
            });

        return request;
    }

    /// <summary>
    /// Gets a parameter schema from the actual serialized Gemini HTTP
    /// request body.
    /// </summary>
    /// <param name="root">
    /// The root JSON element of the outgoing HTTP body.
    /// </param>
    /// <param name="parameterName">
    /// The requested parameter name.
    /// </param>
    /// <returns>
    /// The parameter schema JSON element.
    /// </returns>
    private static JsonElement GetParameterProperty(
        JsonElement root,
        string parameterName)
    {
        var tools =
            root.GetProperty(
                "tools");

        tools.GetArrayLength()
            .Should()
            .BeGreaterThan(
                0);

        var tool =
            tools[0];

        var declarations =
            tool.GetProperty(
                "function_declarations");

        declarations.GetArrayLength()
            .Should()
            .BeGreaterThan(
                0);

        var parameters =
            declarations[0]
                .GetProperty(
                    "parameters");

        var properties =
            parameters.GetProperty(
                "properties");

        properties.TryGetProperty(
                parameterName,
                out var parameter)
            .Should()
            .BeTrue();

        return parameter;
    }
}