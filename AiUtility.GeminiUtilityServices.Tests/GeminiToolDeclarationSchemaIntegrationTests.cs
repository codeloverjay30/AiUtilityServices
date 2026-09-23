using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Mappers;
using AiUtility.GeminiKits.Models;
using AiUtility.GeminiKits.Services;
using AiUtility.GeminiUtilityServices.Services;
using EnumUtilityServices;
using FluentAssertions;
using JsonUtilityServices;
using Moq;
using System.Reflection;
using TypeUtilityServices;

namespace AiUtility.GeminiUtilityServices.Tests;

/// <summary>
/// Contains integration tests for Gemini tool declaration schema generation.
/// </summary>
public sealed class GeminiToolDeclarationSchemaIntegrationTests
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
    /// <see cref="GeminiToolDeclarationSchemaIntegrationTests"/> class.
    /// </summary>
    public GeminiToolDeclarationSchemaIntegrationTests()
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
    /// Verifies that a complex CLR parameter is converted into a Gemini
    /// object schema instead of the legacy "other" schema type.
    /// </summary>
    [Fact]
    public void ToToolDeclaration_ComplexParameter_ShouldGenerateObjectSchema()
    {
        // Arrange
        var metadata =
            CreateMetadata(
                nameof(TestTool.ExecuteComplex));

        // Act
        var declaration =
            _converter.ToToolDeclaration(
                metadata);

        // Assert
        declaration.Parameters.Should()
            .NotBeNull();

        declaration.Parameters.Properties.Should()
            .ContainKey(
                "target");

        var target =
            declaration.Parameters.Properties[
                "target"];

        target.Should()
            .BeOfType<GeminiParameterProperty>();

        target.Type.Should()
            .Be(
                "object");

        target.Type.Should()
            .NotBe(
                "other");

        target.Properties.Should()
            .NotBeNull();

        target.Properties.Should()
            .ContainKey(
                "x");

        target.Properties.Should()
            .ContainKey(
                "y");

        var x =
            target.Properties![
                "x"];

        var y =
            target.Properties[
                "y"];

        x.Type.Should()
            .Be(
                "integer");

        y.Type.Should()
            .Be(
                "integer");

        target.Required.Should()
            .NotBeNull();

        target.Required.Should()
            .Contain(
                "x");

        target.Required.Should()
            .Contain(
                "y");
    }

    /// <summary>
    /// Verifies that a collection of complex CLR objects is converted into
    /// an array whose item schema is a Gemini object schema.
    /// </summary>
    [Fact]
    public void ToToolDeclaration_ComplexCollectionParameter_ShouldGenerateArrayOfObjectsSchema()
    {
        // Arrange
        var metadata =
            CreateMetadata(
                nameof(TestTool.ExecuteCollection));

        // Act
        var declaration =
            _converter.ToToolDeclaration(
                metadata);

        // Assert
        declaration.Parameters.Should()
            .NotBeNull();

        declaration.Parameters.Properties.Should()
            .ContainKey(
                "targets");

        var targets =
            declaration.Parameters.Properties[
                "targets"];

        targets.Type.Should()
            .Be(
                "array");

        targets.Items.Should()
            .NotBeNull();

        targets.Items.Should()
            .BeOfType<GeminiParameterProperty>();

        var item =
            targets.Items!;

        item.Type.Should()
            .Be(
                "object");

        item.Type.Should()
            .NotBe(
                "other");

        item.Properties.Should()
            .NotBeNull();

        item.Properties.Should()
            .ContainKey(
                "x");

        item.Properties.Should()
            .ContainKey(
                "y");

        var x =
            item.Properties![
                "x"];

        var y =
            item.Properties[
                "y"];

        x.Type.Should()
            .Be(
                "integer");

        y.Type.Should()
            .Be(
                "integer");

        item.Required.Should()
            .NotBeNull();

        item.Required.Should()
            .Contain(
                "x");

        item.Required.Should()
            .Contain(
                "y");
    }

    /// <summary>
    /// Verifies that primitive CLR parameters retain their expected Gemini
    /// primitive schema types.
    /// </summary>
    [Fact]
    public void ToToolDeclaration_PrimitiveParameters_ShouldPreservePrimitiveSchemaTypes()
    {
        // Arrange
        var metadata =
            CreateMetadata(
                nameof(TestTool.ExecutePrimitive));

        // Act
        var declaration =
            _converter.ToToolDeclaration(
                metadata);

        // Assert
        declaration.Parameters.Should()
            .NotBeNull();

        declaration.Parameters.Properties.Should()
            .ContainKeys(
                "name",
                "count",
                "enabled");

        var name =
            declaration.Parameters.Properties[
                "name"];

        var count =
            declaration.Parameters.Properties[
                "count"];

        var enabled =
            declaration.Parameters.Properties[
                "enabled"];

        name.Type.Should()
            .Be(
                "string");

        count.Type.Should()
            .Be(
                "integer");

        enabled.Type.Should()
            .Be(
                "boolean");
    }

    /// <summary>
    /// Verifies that nullable object properties are excluded from the
    /// generated required-property collection.
    /// </summary>
    [Fact]
    public void ToToolDeclaration_ComplexParameterWithNullableProperty_ShouldExcludeNullablePropertyFromRequired()
    {
        // Arrange
        var metadata =
            CreateMetadata(
                nameof(TestTool.ExecuteNullableComplex));

        // Act
        var declaration =
            _converter.ToToolDeclaration(
                metadata);

        // Assert
        declaration.Parameters.Should()
            .NotBeNull();

        declaration.Parameters.Properties.Should()
            .ContainKey(
                "target");

        var target =
            declaration.Parameters.Properties[
                "target"];

        target.Type.Should()
            .Be(
                "object");

        target.Properties.Should()
            .NotBeNull();

        target.Properties.Should()
            .ContainKey(
                "requiredvalue");

        target.Properties.Should()
            .ContainKey(
                "optionalvalue");
        

        target.Required.Should()
            .NotBeNull();
    

        target.Required.Should()
            .Contain(
                "requiredvalue");
        

        target.Required.Should()
            .NotContain(
                "optionalvalue");
        
    }

    /// <summary>
    /// Creates Gemini tool metadata for the specified test method.
    /// </summary>
    /// <param name="methodName">
    /// The name of the test method.
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

        var parameters =
            method!.GetParameters();

        return new GeminiToolMetadata(
            name:
                methodName,
            mi:
                method,
            p:
                parameters,
            fi:
                static (_, _) =>
                    null,
            fac:
                null,
            methodAttrs:
                method.GetCustomAttributes());
    }

    /// <summary>
    /// Configures primitive JSON type mappings used by the schema generator.
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
    /// Configures enum discovery for all parameter types used by the tests.
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
    /// Provides methods used to produce reflection metadata for integration
    /// tests.
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
    /// Represents a complex CLR type used to validate nested Gemini schemas.
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
