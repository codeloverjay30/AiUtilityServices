extern alias TypeAlias;

using AiUtility.GeminiUtilityServices.Services;
using AiUtility.ToolKits.Models;
using FluentAssertions;
using JsonUtilityServices;
using Moq;
using System.ComponentModel;
using System.Text.Json.Serialization;
using TypeConstants =
    TypeAlias::CommonConstants.Types.TypeConstants;
using TypeUtilityServices;

namespace AiUtility.GeminiUtilityServices.Tests;

/// <summary>
/// Contains unit tests for strongly typed Gemini schema generation.
/// </summary>
public sealed class GeminiSchemaGeneratorStronglyTypedSchemaTests
{
    private readonly Mock<IJsonUtilityService>
        _jsonUtilityServiceMock;

    private readonly ITypeUtilityService
        _typeUtilityService;

    private readonly GeminiSchemaGenerator
        _sut;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GeminiSchemaGeneratorStronglyTypedSchemaTests"/> class.
    /// </summary>
    public GeminiSchemaGeneratorStronglyTypedSchemaTests()
    {
        _jsonUtilityServiceMock =
            new Mock<IJsonUtilityService>(
                MockBehavior.Strict);

        _typeUtilityService =
            new TypeUtilityService();

        _sut =
            new GeminiSchemaGenerator(
                _jsonUtilityServiceMock.Object,
                _typeUtilityService);
    }

    /// <summary>
    /// Verifies that a simple CLR type is converted to a strongly typed schema.
    /// </summary>
    [Fact]
    public void Generate_SimpleType_ShouldReturnStronglyTypedSchema()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var result =
            _sut.Generate(
                typeof(int));

        // Assert
        result.Should()
            .NotBeNull();

        result.Should()
            .BeOfType<AiParameterPropertyBase>();

        result.Type.Should()
            .Be(
                TypeConstants.INT);

        result.Properties.Should()
            .BeNull();

        result.Items.Should()
            .BeNull();
    }

    /// <summary>
    /// Verifies that an array is represented by an array schema
    /// containing the correct element schema.
    /// </summary>
    [Fact]
    public void Generate_Array_ShouldGenerateArraySchemaWithItems()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var result =
            _sut.Generate(
                typeof(int[]));

        // Assert
        result.Type.Should()
            .Be(
                TypeConstants.ARRAY);

        result.Items.Should()
            .NotBeNull();

        result.Items!.Type.Should()
            .Be(
                TypeConstants.INT);

        result.Properties.Should()
            .BeNull();
    }

    /// <summary>
    /// Verifies that a generic collection is represented by an array schema.
    /// </summary>
    [Fact]
    public void Generate_GenericCollection_ShouldGenerateArraySchemaWithItems()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(string)))
            .Returns(
                TypeConstants.STRING);

        // Act
        var result =
            _sut.Generate(
                typeof(List<string>));

        // Assert
        result.Type.Should()
            .Be(
                TypeConstants.ARRAY);

        result.Items.Should()
            .NotBeNull();

        result.Items!.Type.Should()
            .Be(
                TypeConstants.STRING);
    }

    /// <summary>
    /// Verifies that a complex CLR type is represented by an object schema.
    /// </summary>
    [Fact]
    public void Generate_ComplexType_ShouldGenerateObjectSchema()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(string)))
            .Returns(
                TypeConstants.STRING);

        // Act
        var result =
            _sut.Generate(
                typeof(TestTarget));

        // Assert
        result.Type.Should()
            .Be(
                TypeConstants.OBJECT);

        result.Properties.Should()
            .NotBeNull();

        result.Properties.Should()
            .ContainKey(
                "x");

        result.Properties.Should()
            .ContainKey(
                "name");

        result.Properties!["x"]
            .Type.Should()
            .Be(
                TypeConstants.INT);

        result.Properties["name"]
            .Type.Should()
            .Be(
                TypeConstants.STRING);
    }

    /// <summary>
    /// Verifies that complex CLR types do not fall back to the unsupported
    /// "other" schema type.
    /// </summary>
    [Fact]
    public void Generate_ComplexType_ShouldNotGenerateOtherSchemaType()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(string)))
            .Returns(
                TypeConstants.STRING);

        // Act
        var result =
            _sut.Generate(
                typeof(TestTarget));

        // Assert
        result.Type.Should()
            .Be(
                TypeConstants.OBJECT);

        result.Type.Should()
            .NotBe(
                "other");
    }

    /// <summary>
    /// Verifies that property descriptions are preserved in generated schemas.
    /// </summary>
    [Fact]
    public void Generate_PropertyWithDescription_ShouldPreserveDescription()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var result =
            _sut.Generate(
                typeof(DescribedTarget));

        // Assert
        result.Properties.Should()
            .NotBeNull();

        result.Properties.Should()
            .ContainKey(
                "x");

        result.Properties!["x"]
            .Description.Should()
            .Be(
                "Horizontal coordinate.");
    }

    /// <summary>
    /// Verifies that properties marked with JsonIgnore are excluded
    /// from generated schemas.
    /// </summary>
    [Fact]
    public void Generate_PropertyWithJsonIgnore_ShouldExcludeProperty()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var result =
            _sut.Generate(
                typeof(TargetWithIgnoredProperty));

        // Assert
        result.Properties.Should()
            .NotBeNull();

        result.Properties.Should()
            .ContainKey(
                "visible");

        result.Properties.Should()
            .NotContainKey(
                "ignored");
    }

    /// <summary>
    /// Verifies that non-nullable properties are included
    /// in the required property collection.
    /// </summary>
    [Fact]
    public void Generate_NonNullableProperty_ShouldAddPropertyToRequired()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var result =
            _sut.Generate(
                typeof(RequiredTarget));

        // Assert
        result.Required.Should()
            .NotBeNull();

        result.Required.Should()
            .Contain(
                "value");
    }

    /// <summary>
    /// Verifies that nested complex objects are generated recursively.
    /// </summary>
    [Fact]
    public void Generate_NestedComplexType_ShouldGenerateNestedObjectSchema()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var result =
            _sut.Generate(
                typeof(ContainerTarget));

        // Assert
        result.Properties.Should()
            .NotBeNull();

        result.Properties.Should()
            .ContainKey(
                "target");

        var nestedTarget =
            result.Properties!["target"];

        nestedTarget.Type.Should()
            .Be(
                TypeConstants.OBJECT);

        nestedTarget.Properties.Should()
            .NotBeNull();

        nestedTarget.Properties.Should()
            .ContainKey(
                "x");

        nestedTarget.Properties!["x"]
            .Type.Should()
            .Be(
                TypeConstants.INT);
    }

    /// <summary>
    /// Verifies that collections containing complex objects generate
    /// an object schema for their element type.
    /// </summary>
    [Fact]
    public void Generate_ComplexCollection_ShouldGenerateObjectItemSchema()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var result =
            _sut.Generate(
                typeof(List<CoordinateTarget>));

        // Assert
        result.Type.Should()
            .Be(
                TypeConstants.ARRAY);

        result.Items.Should()
            .NotBeNull();

        result.Items!.Type.Should()
            .Be(
                TypeConstants.OBJECT);

        result.Items.Properties.Should()
            .NotBeNull();

        result.Items.Properties.Should()
            .ContainKey(
                "x");

        result.Items.Properties.Should()
            .ContainKey(
                "y");
    }

    /// <summary>
    /// Verifies that a null type is rejected at the public service boundary.
    /// </summary>
    [Fact]
    public void Generate_NullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        Type type =
            null!;

        // Act
        Action act =
            () =>
                _sut.Generate(
                    type);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage(
                "*type*");
    }

    /// <summary>
    /// Verifies that generated schemas are cached by CLR type.
    /// </summary>
    [Fact]
    public void Generate_SameTypeTwice_ShouldReturnCachedSchemaInstance()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var first =
            _sut.Generate(
                typeof(int));

        var second =
            _sut.Generate(
                typeof(int));

        // Assert
        second.Should()
            .BeSameAs(
                first);

        _sut.Cache.Should()
            .ContainKey(
                typeof(int));

        _jsonUtilityServiceMock.Verify(
            service =>
                service.GetJsonType(
                    typeof(int)),
            Times.Once);
    }

    /// <summary>
    /// Verifies that the generic Generate overload generates the schema
    /// for its generic CLR type.
    /// </summary>
    [Fact]
    public void Generate_GenericOverload_ShouldGenerateSchemaForGenericType()
    {
        // Arrange
        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                TypeConstants.INT);

        // Act
        var result =
            _sut.Generate<int>();

        // Assert
        result.Type.Should()
            .Be(
                TypeConstants.INT);

        _sut.Cache.Should()
            .ContainKey(
                typeof(int));
    }

    private sealed class TestTarget
    {
        public int X { get; set; }

        public string Name { get; set; } =
            string.Empty;
    }

    private sealed class DescribedTarget
    {
        [Description("Horizontal coordinate.")]
        public int X { get; set; }
    }

    private sealed class TargetWithIgnoredProperty
    {
        public int Visible { get; set; }

        [JsonIgnore]
        public int Ignored { get; set; }
    }

    private sealed class RequiredTarget
    {
        public int Value { get; set; }
    }

    private sealed class ContainerTarget
    {
        public CoordinateTarget Target { get; set; } =
            new();
    }

    private sealed class CoordinateTarget
    {
        public int X { get; set; }

        public int Y { get; set; }
    }
}