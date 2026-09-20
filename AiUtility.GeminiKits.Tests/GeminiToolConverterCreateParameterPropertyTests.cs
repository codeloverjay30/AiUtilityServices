using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Mappers;
using AiUtility.GeminiKits.Models;
using AiUtility.GeminiKits.Services;
using AiUtility.ToolKits.Models;
using AiUtility.ToolKits.Services;
using EnumUtilityServices;
using FluentAssertions;
using JsonUtilityServices;
using Moq;
using System.ComponentModel;
using System.Reflection;

namespace AiUtility.GeminiKits.Tests;

/// <summary>
/// Contains tests for Gemini parameter property creation.
/// </summary>
public sealed class GeminiToolConverterCreateParameterPropertyTests
{
    private const string DefaultParameterDescription =
        "Default parameter description.";

    private readonly Mock<IJsonUtilityService>
        _jsonUtilityServiceMock;

    private readonly Mock<IEnumUtilityService>
        _enumUtilityServiceMock;

    private readonly Mock<IAiParameterSchemaGenerator>
        _parameterSchemaGeneratorMock;

    private readonly Mock<IGeminiParameterPropertyMapper>
        _parameterPropertyMapperMock;

    private readonly TestableGeminiToolConverter
        _sut;

    /// <summary>
    /// Initializes a new instance of the test class.
    /// </summary>
    public GeminiToolConverterCreateParameterPropertyTests()
    {
        _jsonUtilityServiceMock =
            new Mock<IJsonUtilityService>(
                MockBehavior.Strict);

        _enumUtilityServiceMock =
            new Mock<IEnumUtilityService>(
                MockBehavior.Strict);

        _parameterSchemaGeneratorMock =
            new Mock<IAiParameterSchemaGenerator>(
                MockBehavior.Strict);

        _parameterPropertyMapperMock =
            new Mock<IGeminiParameterPropertyMapper>(
                MockBehavior.Strict);

        _sut =
            new TestableGeminiToolConverter(
                _jsonUtilityServiceMock.Object,
                _enumUtilityServiceMock.Object,
                _parameterSchemaGeneratorMock.Object,
                _parameterPropertyMapperMock.Object,
                DefaultParameterDescription);
    }

    /// <summary>
    /// Verifies that a primitive parameter uses the generated
    /// schema and mapped Gemini property.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_PrimitiveParameter_ShouldUseGeneratedSchema()
    {
        // Arrange
        var parameter =
            GetParameter(
                nameof(TestMethods.Primitive));

        var schema =
            new AiParameterPropertyBase
            {
                Type = "integer"
            };

        var mappedProperty =
            new GeminiParameterProperty
            {
                Type = "integer"
            };

        _parameterSchemaGeneratorMock
            .Setup(
                generator =>
                    generator.Generate(
                        typeof(int)))
            .Returns(
                schema);

        _parameterPropertyMapperMock
            .Setup(
                mapper =>
                    mapper.Map(
                        schema))
            .Returns(
                mappedProperty);

        // Act
        var result =
            _sut.InvokeCreateParameterProperty(
                parameter);

        // Assert
        result.Should()
            .BeSameAs(
                mappedProperty);

        result.Type.Should()
            .Be(
                "integer");

        _parameterSchemaGeneratorMock.Verify(
            generator =>
                generator.Generate(
                    typeof(int)),
            Times.Once);

        _parameterPropertyMapperMock.Verify(
            mapper =>
                mapper.Map(
                    schema),
            Times.Once);
    }

    /// <summary>
    /// Verifies that a complex parameter preserves its nested
    /// object schema after mapping.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_ComplexParameter_ShouldReturnObjectSchema()
    {
        // Arrange
        var parameter =
            GetParameter(
                nameof(TestMethods.Complex));

        var schema =
            new AiParameterPropertyBase
            {
                Type = "object",

                Properties =
                    new Dictionary<
                        string,
                        AiParameterPropertyBase>
                    {
                        ["x"] =
                            new()
                            {
                                Type = "integer"
                            },

                        ["y"] =
                            new()
                            {
                                Type = "integer"
                            }
                    },

                Required =
                [
                    "x",
                    "y"
                ]
            };

        var mappedProperty =
            new GeminiParameterProperty
            {
                Type = "object",

                Properties =
                    new Dictionary<
                        string,
                        GeminiParameterProperty>
                    {
                        ["x"] =
                            new GeminiParameterProperty
                            {
                                Type = "integer"
                            },

                        ["y"] =
                            new GeminiParameterProperty
                            {
                                Type = "integer"
                            }
                    },

                Required =
                [
                    "x",
                    "y"
                ]
            };

        _parameterSchemaGeneratorMock
            .Setup(
                generator =>
                    generator.Generate(
                        typeof(TestTarget)))
            .Returns(
                schema);

        _parameterPropertyMapperMock
            .Setup(
                mapper =>
                    mapper.Map(
                        schema))
            .Returns(
                mappedProperty);

        // Act
        var result =
            _sut.InvokeCreateParameterProperty(
                parameter);

        // Assert
        result.Type.Should()
            .Be(
                "object");

        result.Type.Should()
            .NotBe(
                "other");

        result.Properties.Should()
            .NotBeNull();

        result.Properties.Should()
            .ContainKey(
                "x");

        result.Properties.Should()
            .ContainKey(
                "y");

        result.Required.Should()
            .BeEquivalentTo(
                [
                    "x",
                    "y"
                ]);

        _parameterSchemaGeneratorMock.Verify(
            generator =>
                generator.Generate(
                    typeof(TestTarget)),
            Times.Once);

        _parameterPropertyMapperMock.Verify(
            mapper =>
                mapper.Map(
                    schema),
            Times.Once);
    }

    /// <summary>
    /// Verifies that a collection parameter preserves its array
    /// item schema after mapping.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_CollectionParameter_ShouldReturnArraySchema()
    {
        // Arrange
        var parameter =
            GetParameter(
                nameof(TestMethods.Collection));

        var schema =
            new AiParameterPropertyBase
            {
                Type = "array",

                Items =
                    new AiParameterPropertyBase
                    {
                        Type = "object"
                    }
            };

        var mappedProperty =
            new GeminiParameterProperty
            {
                Type = "array",

                Items =
                    new GeminiParameterProperty
                    {
                        Type = "object"
                    }
            };

        _parameterSchemaGeneratorMock
            .Setup(
                generator =>
                    generator.Generate(
                        typeof(List<TestTarget>)))
            .Returns(
                schema);

        _parameterPropertyMapperMock
            .Setup(
                mapper =>
                    mapper.Map(
                        schema))
            .Returns(
                mappedProperty);

        // Act
        var result =
            _sut.InvokeCreateParameterProperty(
                parameter);

        // Assert
        result.Type.Should()
            .Be(
                "array");

        result.Items.Should()
            .NotBeNull();

        result.Items!.Should()
            .BeOfType<GeminiParameterProperty>();

        result.Items.Type.Should()
            .Be(
                "object");
    }

    /// <summary>
    /// Verifies that a parameter description overrides the
    /// description supplied by the generated schema.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_ParameterWithDescription_ShouldOverrideSchemaDescription()
    {
        // Arrange
        var parameter =
            GetParameter(
                nameof(TestMethods.WithDescription));

        var schema =
            new AiParameterPropertyBase
            {
                Type = "integer",
                Description = "Schema description."
            };

        var mappedProperty =
            new GeminiParameterProperty
            {
                Type = "integer",
                Description = "Schema description."
            };

        _parameterSchemaGeneratorMock
            .Setup(
                generator =>
                    generator.Generate(
                        typeof(int)))
            .Returns(
                schema);

        _parameterPropertyMapperMock
            .Setup(
                mapper =>
                    mapper.Map(
                        schema))
            .Returns(
                mappedProperty);

        // Act
        var result =
            _sut.InvokeCreateParameterProperty(
                parameter);

        // Assert
        result.Description.Should()
            .Be(
                "Parameter description.");
    }

    /// <summary>
    /// Verifies that a parameter without a description uses
    /// the configured default parameter description.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_ParameterWithoutDescription_ShouldUseDefaultDescription()
    {
        // Arrange
        var parameter =
            GetParameter(
                nameof(TestMethods.Primitive));

        var schema =
            new AiParameterPropertyBase
            {
                Type = "integer"
            };

        var mappedProperty =
            new GeminiParameterProperty
            {
                Type = "integer"
            };

        _parameterSchemaGeneratorMock
            .Setup(
                generator =>
                    generator.Generate(
                        typeof(int)))
            .Returns(
                schema);

        _parameterPropertyMapperMock
            .Setup(
                mapper =>
                    mapper.Map(
                        schema))
            .Returns(
                mappedProperty);

        // Act
        var result =
            _sut.InvokeCreateParameterProperty(
                parameter);

        // Assert
        result.Description.Should()
            .Be(
                DefaultParameterDescription);
    }

    /// <summary>
    /// Verifies that a null parameter is rejected before
    /// schema generation begins.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_NullParameter_ShouldThrowArgumentNullException()
    {
        // Arrange
        ParameterInfo parameter =
            null!;

        // Act
        Action act =
            () =>
                _sut.InvokeCreateParameterProperty(
                    parameter);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage(
                "*parameter*");

        _parameterSchemaGeneratorMock.VerifyNoOtherCalls();
        _parameterPropertyMapperMock.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Gets the first parameter of the specified test method.
    /// </summary>
    /// <param name="methodName">
    /// The name of the test method.
    /// </param>
    /// <returns>
    /// The reflected parameter metadata.
    /// </returns>
    private static ParameterInfo GetParameter(
        string methodName)
    {
        var method =
            typeof(TestMethods).GetMethod(
                methodName,
                BindingFlags.Public |
                BindingFlags.Static);

        method.Should()
            .NotBeNull();

        var parameters =
            method!.GetParameters();

        parameters.Should()
            .ContainSingle();

        return parameters[0];
    }

    private static class TestMethods
    {
        /// <summary>
        /// Provides a primitive parameter for reflection-based tests.
        /// </summary>
        /// <param name="value">
        /// The test value.
        /// </param>
        public static void Primitive(
            int value)
        {
        }

        /// <summary>
        /// Provides a complex parameter for reflection-based tests.
        /// </summary>
        /// <param name="target">
        /// The test target.
        /// </param>
        public static void Complex(
            TestTarget target)
        {
        }

        /// <summary>
        /// Provides a collection parameter for reflection-based tests.
        /// </summary>
        /// <param name="targets">
        /// The test targets.
        /// </param>
        public static void Collection(
            List<TestTarget> targets)
        {
        }

        /// <summary>
        /// Provides a described parameter for reflection-based tests.
        /// </summary>
        /// <param name="value">
        /// The test value.
        /// </param>
        public static void WithDescription(
            [Description("Parameter description.")]
            int value)
        {
        }
    }

    private sealed class TestTarget
    {
        public int X { get; set; }

        public int Y { get; set; }
    }

    /// <summary>
    /// Exposes protected Gemini tool converter behavior for unit testing.
    /// </summary>
    private sealed class TestableGeminiToolConverter
        : GeminiToolConverter
    {
        /// <summary>
        /// Initializes a new instance of the test converter.
        /// </summary>
        /// <param name="jsonUtilityService">
        /// The JSON utility service.
        /// </param>
        /// <param name="enumUtilityService">
        /// The enum utility service.
        /// </param>
        /// <param name="parameterSchemaGenerator">
        /// The parameter schema generator.
        /// </param>
        /// <param name="parameterPropertyMapper">
        /// The Gemini parameter property mapper.
        /// </param>
        /// <param name="defaultParameterDescription">
        /// The default parameter description.
        /// </param>
        public TestableGeminiToolConverter(
            IJsonUtilityService jsonUtilityService,
            IEnumUtilityService enumUtilityService,
            IAiParameterSchemaGenerator parameterSchemaGenerator,
            IGeminiParameterPropertyMapper parameterPropertyMapper,
            string defaultParameterDescription)
            : base(
                jsonUtilityService,
                enumUtilityService,
                parameterSchemaGenerator,
                parameterPropertyMapper,
                defaultParameterDescription:
                    defaultParameterDescription)
        {
        }

        /// <summary>
        /// Invokes the protected parameter property creation method.
        /// </summary>
        /// <param name="parameter">
        /// The parameter metadata.
        /// </param>
        /// <returns>
        /// The generated Gemini parameter property.
        /// </returns>
        public GeminiParameterProperty InvokeCreateParameterProperty(
            ParameterInfo parameter)
        {
            return CreateParameterProperty(
                parameter);
        }
    }
}