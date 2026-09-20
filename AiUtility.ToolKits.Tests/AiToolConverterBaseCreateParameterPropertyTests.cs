using AiUtility.ToolKits.Models;
using AiUtility.ToolKits.Services;
using EnumUtilityServices;
using FluentAssertions;
using JsonUtilityServices;
using Moq;
using System.ComponentModel;
using System.Reflection;

namespace AiUtility.ToolKits.Tests;

/// <summary>
/// Contains unit tests for the CreateParameterProperty extension point
/// of <see cref="AiToolConverterBase{TAttribute,TDeclaration,TParameters,TProperty}"/>.
/// </summary>
public sealed class AiToolConverterBaseCreateParameterPropertyTests
{
    private const string DefaultParameterDescription =
        "Default parameter description";

    private readonly Mock<IJsonUtilityService>
        _jsonUtilityServiceMock;

    private readonly Mock<IEnumUtilityService>
        _enumUtilityServiceMock;

    private readonly TestAiToolConverter
        _sut;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AiToolConverterBaseCreateParameterPropertyTests"/> class.
    /// </summary>
    public AiToolConverterBaseCreateParameterPropertyTests()
    {
        _jsonUtilityServiceMock =
            new Mock<IJsonUtilityService>(
                MockBehavior.Strict);

        _enumUtilityServiceMock =
            new Mock<IEnumUtilityService>(
                MockBehavior.Strict);

        _sut =
            new TestAiToolConverter(
                _jsonUtilityServiceMock.Object,
                _enumUtilityServiceMock.Object,
                DefaultParameterDescription);
    }

    /// <summary>
    /// Verifies that the parameter type is mapped through the JSON utility service.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_ValidParameter_ShouldMapParameterType()
    {
        // Arrange
        var parameter =
            GetParameter(
                nameof(TestMethods.WithoutDescription));

        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                "integer");

        // Act
        var result =
            _sut.InvokeCreateParameterProperty(
                parameter);

        // Assert
        result.Type.Should()
            .Be(
                "integer");

        _jsonUtilityServiceMock.Verify(
            service =>
                service.GetJsonType(
                    typeof(int)),
            Times.Once);
    }

    /// <summary>
    /// Verifies that a description attribute is used as the parameter description.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_ParameterWithDescription_ShouldUseDescriptionAttribute()
    {
        // Arrange
        var parameter =
            GetParameter(
                nameof(TestMethods.WithDescription));

        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(string)))
            .Returns(
                "string");

        // Act
        var result =
            _sut.InvokeCreateParameterProperty(
                parameter);

        // Assert
        result.Description.Should()
            .Be(
                "The test value.");
    }

    /// <summary>
    /// Verifies that the configured default description is used when
    /// the parameter does not define a description attribute.
    /// </summary>
    [Fact]
    public void CreateParameterProperty_ParameterWithoutDescription_ShouldUseDefaultDescription()
    {
        // Arrange
        var parameter =
            GetParameter(
                nameof(TestMethods.WithoutDescription));

        _jsonUtilityServiceMock
            .Setup(
                service =>
                    service.GetJsonType(
                        typeof(int)))
            .Returns(
                "integer");

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
    /// Verifies that a null parameter is rejected before schema generation.
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
    }

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

    private sealed class TestAiToolConverter
        : AiToolConverterBase<
            TestToolAttribute,
            TestDeclaration,
            TestParameters,
            TestProperty>
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="TestAiToolConverter"/> class.
        /// </summary>
        public TestAiToolConverter(
            IJsonUtilityService jsonUtilityService,
            IEnumUtilityService enumUtilityService,
            string defaultParameterDescription)
            : base(
                jsonUtilityService,
                enumUtilityService,
                defaultParameterDescription:
                    defaultParameterDescription)
        {
        }

        /// <summary>
        /// Exposes parameter property creation for unit testing.
        /// </summary>
        /// <param name="parameter">
        /// The parameter metadata to convert.
        /// </param>
        /// <returns>
        /// The generated test parameter property.
        /// </returns>
        public TestProperty InvokeCreateParameterProperty(
            ParameterInfo parameter)
        {
            return CreateParameterProperty(
                parameter);
        }

        /// <summary>
        /// Gets the description represented by the test attribute.
        /// </summary>
        /// <param name="attr">
        /// The test tool attribute.
        /// </param>
        /// <returns>
        /// The configured description, or <see langword="null"/>.
        /// </returns>
        protected override string? GetDescriptionFromAttribute(
            TestToolAttribute? attr)
        {
            return attr?.Description;
        }
    }

    [AttributeUsage(
        AttributeTargets.Method)]
    private sealed class TestToolAttribute
        : Attribute
    {
        public string? Description { get; init; }
    }

    private sealed class TestDeclaration
        : AiToolDeclarationBase
    {
    }

    private sealed class TestParameters
        : AiParametersBase
    {
    }

    private sealed class TestProperty
        : AiParameterPropertyBase
    {
    }

    private static class TestMethods
    {
        public static void WithoutDescription(
            int value)
        {
        }

        public static void WithDescription(
            [Description("The test value.")]
            string value)
        {
        }
    }
}