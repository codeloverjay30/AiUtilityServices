using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Mappers;
using AiUtility.GeminiKits.Models;
using AiUtility.GeminiKits.Services;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using EnumUtilityServices;
using FluentAssertions;
using JsonUtilityServices;
using Moq;
using System.Reflection;
using System.Text.Json;
using TypeUtilityServices;
using static AiUtility.GeminiUtilityServices.Models.GeminiGenerateRequest;

namespace AiUtility.GeminiUtilityServices.Tests;

/// <summary>
/// Contains integration tests that verify Gemini tool declarations are
/// serialized into the expected Gemini wire JSON contract.
/// </summary>
public sealed class GeminiToolDeclarationWireIntegrationTests
{
    private readonly Mock<IJsonUtilityService>
        _jsonUtilityServiceMock;

    private readonly Mock<IEnumUtilityService>
        _enumUtilityServiceMock;

    private readonly ITypeUtilityService
        _typeUtilityService;

    private readonly GeminiSchemaGenerator
        _schemaGenerator;

    private readonly GeminiParameterPropertyMapper
        _parameterPropertyMapper;

    private readonly GeminiToolConverter
        _converter;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GeminiToolDeclarationWireIntegrationTests"/> class.
    /// </summary>
    public GeminiToolDeclarationWireIntegrationTests()
    {
        _jsonUtilityServiceMock =
            new Mock<IJsonUtilityService>(
                MockBehavior.Strict);

        _enumUtilityServiceMock =
            new Mock<IEnumUtilityService>(
                MockBehavior.Strict);

        _typeUtilityService =
            new TypeUtilityService();

        ConfigureJsonTypeMappings();
        ConfigureEnumMappings();

        _schemaGenerator =
            new GeminiSchemaGenerator(
                _jsonUtilityServiceMock.Object,
                _typeUtilityService);

        _parameterPropertyMapper =
            new GeminiParameterPropertyMapper();

        _converter =
            new GeminiToolConverter(
                jsonUtilityService:
                    _jsonUtilityServiceMock.Object,
                enumUtilityService:
                    _enumUtilityServiceMock.Object,
                parameterSchemaGenerator:
                    _schemaGenerator,
                parameterPropertyMapper:
                    _parameterPropertyMapper);
    }

    /// <summary>
    /// Verifies that a complex CLR parameter reaches the Gemini wire
    /// contract as an object schema rather than the legacy other type.
    /// </summary>
    [Fact]
    public void ToGoogleApiRequest_ComplexToolParameter_ShouldSerializeAsObjectSchema()
    {
        // Arrange
        using var document =
            CreateWireDocument(
                nameof(TestTool.ExecuteComplex));

        // Act
        var target =
            GetParameterProperty(
                document.RootElement,
                "target");

        // Assert
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
    /// Verifies that a collection of complex CLR objects reaches the Gemini
    /// wire contract as an array containing object item schemas.
    /// </summary>
    [Fact]
    public void ToGoogleApiRequest_ComplexCollectionParameter_ShouldSerializeAsArrayOfObjectSchemas()
    {
        // Arrange
        using var document =
            CreateWireDocument(
                nameof(TestTool.ExecuteCollection));

        // Act
        var targets =
            GetParameterProperty(
                document.RootElement,
                "targets");

        // Assert
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
    /// Verifies that primitive CLR parameters retain their expected Gemini
    /// schema types after final wire serialization.
    /// </summary>
    [Fact]
    public void ToGoogleApiRequest_PrimitiveParameters_ShouldPreservePrimitiveWireTypes()
    {
        // Arrange
        using var document =
            CreateWireDocument(
                nameof(TestTool.ExecutePrimitive));

        // Act
        var root =
            document.RootElement;

        var name =
            GetParameterProperty(
                root,
                "name");

        var count =
            GetParameterProperty(
                root,
                "count");

        var enabled =
            GetParameterProperty(
                root,
                "enabled");

        // Assert
        name.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "string");

        count.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "integer");

        enabled.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "boolean");
    }

    /// <summary>
    /// Verifies that required and nullable nested properties retain their
    /// expected required-state semantics in the final Gemini wire contract.
    /// </summary>
    [Fact]
    public void ToGoogleApiRequest_NullableComplexProperty_ShouldSerializeCorrectRequiredCollection()
    {
        // Arrange
        using var document =
            CreateWireDocument(
                nameof(TestTool.ExecuteNullableComplex));

        // Act
        var target =
            GetParameterProperty(
                document.RootElement,
                "target");

        var required =
            target.GetProperty(
                "required")
            .EnumerateArray()
            .Select(
                element =>
                    element.GetString())
            .Where(
                value =>
                    value is not null)
            .Cast<string>()
            .ToArray();

        // Assert
        required.Should()
            .Contain(
                "requiredvalue");

        required.Should()
            .NotContain(
                "optionalvalue");

        var properties =
            target.GetProperty(
                "properties");

        properties.TryGetProperty(
                "requiredvalue",
                out var requiredValue)
            .Should()
            .BeTrue();

        properties.TryGetProperty(
                "optionalvalue",
                out var optionalValue)
            .Should()
            .BeTrue();

        requiredValue.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "integer");

        optionalValue.GetProperty(
                "type")
            .GetString()
            .Should()
            .Be(
                "integer");
    }

    /// <summary>
    /// Creates a serialized Gemini API request containing the tool declaration
    /// generated from the specified test method.
    /// </summary>
    /// <param name="methodName">
    /// The test tool method name.
    /// </param>
    /// <returns>
    /// A JSON document containing the final Gemini wire request.
    /// </returns>
    private JsonDocument CreateWireDocument(
        string methodName)
    {
        var metadata =
            CreateMetadata(
                methodName);

        var declaration =
            _converter.ToToolDeclaration(
                metadata);

        var request =
            new GeminiGenerateRequest();

        request.Tools.Add(
            new GeminiToolDeclarationWrapper
            {
                FunctionDeclarations =
                [
                    declaration,
                ],
            });

        var apiRequest =
            request.ToGoogleApiRequest();

        var json =
            JsonSerializer.Serialize(
                apiRequest);

        return JsonDocument.Parse(
            json);
    }

    /// <summary>
    /// Gets a parameter property from the first Gemini function declaration
    /// contained in the serialized request.
    /// </summary>
    /// <param name="root">
    /// The root Gemini request JSON element.
    /// </param>
    /// <param name="parameterName">
    /// The parameter property name.
    /// </param>
    /// <returns>
    /// The requested parameter schema JSON element.
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
            GetFunctionDeclarations(
                tool);

        declarations.GetArrayLength()
            .Should()
            .BeGreaterThan(
                0);

        var declaration =
            declarations[0];

        var parameters =
            declaration.GetProperty(
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

    /// <summary>
    /// Gets the function declaration collection while preserving compatibility
    /// with the currently configured Gemini wire property name.
    /// </summary>
    /// <param name="tool">
    /// The serialized Gemini tool wrapper.
    /// </param>
    /// <returns>
    /// The function declaration JSON array.
    /// </returns>
    private static JsonElement GetFunctionDeclarations(
        JsonElement tool)
    {
        if (tool.TryGetProperty(
                "functionDeclarations",
                out var declarations))
        {
            return declarations;
        }

        tool.TryGetProperty(
                "function_declarations",
                out declarations)
            .Should()
            .BeTrue();

        return declarations;
    }

    /// <summary>
    /// Creates Gemini tool metadata for the specified test method.
    /// </summary>
    /// <param name="methodName">
    /// The name of the test tool method.
    /// </param>
    /// <returns>
    /// The generated Gemini tool metadata.
    /// </returns>
    private static GeminiToolMetadata CreateMetadata(
        string methodName)
    {
        var method =
            typeof(TestTool).GetMethod(
                methodName,
                BindingFlags.Public |
                BindingFlags.Static);

        method.Should()
            .NotBeNull();

        return new GeminiToolMetadata(
            name:
                methodName,
            mi:
                method!,
            p:
                method.GetParameters(),
            fi:
                static (_, _) =>
                    null,
            fac:
                null,
            methodAttrs:
                method.GetCustomAttributes());
    }

    /// <summary>
    /// Configures primitive JSON type mappings required by the real Gemini
    /// schema generator.
    /// </summary>
    private void ConfigureJsonTypeMappings()
    {
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(string)))
            .Returns(
                "string");

        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                "integer");

        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int?)))
            .Returns(
                "integer");

        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(bool)))
            .Returns(
                "boolean");
    }

    /// <summary>
    /// Configures enum discovery for the parameter types used by the
    /// integration tests.
    /// </summary>
    private void ConfigureEnumMappings()
    {
        _enumUtilityServiceMock
            .Setup(
                service =>
                    service.GetEnumNames(
                        It.IsAny<Type>()))
            .Returns(
                []);
    }

    /// <summary>
    /// Provides methods used to generate reflection metadata for wire
    /// integration tests.
    /// </summary>
    private static class TestTool
    {
        /// <summary>
        /// Accepts a complex test target.
        /// </summary>
        /// <param name="target">
        /// The target object.
        /// </param>
        public static void ExecuteComplex(
            TestTarget target)
        {
        }

        /// <summary>
        /// Accepts a collection of complex test targets.
        /// </summary>
        /// <param name="targets">
        /// The target collection.
        /// </param>
        public static void ExecuteCollection(
            List<TestTarget> targets)
        {
        }

        /// <summary>
        /// Accepts primitive parameters.
        /// </summary>
        /// <param name="name">
        /// The test name.
        /// </param>
        /// <param name="count">
        /// The test count.
        /// </param>
        /// <param name="enabled">
        /// The enabled state.
        /// </param>
        public static void ExecutePrimitive(
            string name,
            int count,
            bool enabled)
        {
        }

        /// <summary>
        /// Accepts a complex object containing nullable and non-nullable
        /// properties.
        /// </summary>
        /// <param name="target">
        /// The nullable-property test target.
        /// </param>
        public static void ExecuteNullableComplex(
            NullableTestTarget target)
        {
        }
    }

    /// <summary>
    /// Represents a complex CLR type used to validate Gemini wire schemas.
    /// </summary>
    private sealed class TestTarget
    {
        /// <summary>
        /// Gets or sets the horizontal coordinate.
        /// </summary>
        public int X
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the vertical coordinate.
        /// </summary>
        public int Y
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents a complex CLR type containing nullable and non-nullable
    /// properties.
    /// </summary>
    private sealed class NullableTestTarget
    {
        /// <summary>
        /// Gets or sets the required value.
        /// </summary>
        public int RequiredValue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the optional value.
        /// </summary>
        public int? OptionalValue
        {
            get;
            set;
        }
    }
}
