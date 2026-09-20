using AiUtility.GeminiKits.Mappers;
using AiUtility.GeminiKits.Models;
using AiUtility.ToolKits.Models;
using FluentAssertions;

namespace AiUtility.GeminiKits.Tests;

/// <summary>
/// Contains unit tests for <see cref="GeminiParameterPropertyMapper"/>.
/// </summary>
public sealed class GeminiParameterPropertyMapperTests
{
    private readonly GeminiParameterPropertyMapper _sut =
        new();

    /// <summary>
    /// Verifies that a simple parameter schema is mapped correctly.
    /// </summary>
    [Fact]
    public void Map_SimpleSchema_ShouldMapScalarProperties()
    {
        // Arrange
        var source =
            new AiParameterPropertyBase
            {
                Type = "string",
                Description = "A test value.",
                Enum =
                [
                    "one",
                    "two"
                ]
            };

        // Act
        var result =
            _sut.Map(
                source);

        // Assert
        result.Should()
            .NotBeNull();

        result.Should()
            .BeOfType<GeminiParameterProperty>();

        result.Type.Should()
            .Be(
                "string");

        result.Description.Should()
            .Be(
                "A test value.");

        result.Enum.Should()
            .BeEquivalentTo(
                [
                    "one",
                    "two"
                ]);

        result.Properties.Should()
            .BeNull();

        result.Required.Should()
            .BeNull();

        result.Items.Should()
            .BeNull();
    }

    /// <summary>
    /// Verifies that an object schema and its nested properties
    /// are mapped recursively.
    /// </summary>
    [Fact]
    public void Map_ObjectSchema_ShouldMapPropertiesAndRequiredRecursively()
    {
        // Arrange
        var source =
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
                                Type = "integer",
                                Description =
                                    "Horizontal coordinate."
                            },

                        ["y"] =
                            new()
                            {
                                Type = "integer",
                                Description =
                                    "Vertical coordinate."
                            }
                    },

                Required =
                [
                    "x",
                    "y"
                ]
            };

        // Act
        var result =
            _sut.Map(
                source);

        // Assert
        result.Type.Should()
            .Be(
                "object");

        result.Properties.Should()
            .NotBeNull();

        result.Properties.Should()
            .HaveCount(
                2);

        result.Properties.Should()
            .ContainKey(
                "x");

        result.Properties.Should()
            .ContainKey(
                "y");

        result.Properties!["x"]
            .Should()
            .BeOfType<GeminiParameterProperty>();

        result.Properties["x"]
            .Type.Should()
            .Be(
                "integer");

        result.Properties["x"]
            .Description.Should()
            .Be(
                "Horizontal coordinate.");

        result.Properties["y"]
            .Should()
            .BeOfType<GeminiParameterProperty>();

        result.Properties["y"]
            .Type.Should()
            .Be(
                "integer");

        result.Required.Should()
            .BeEquivalentTo(
                [
                    "x",
                    "y"
                ]);
    }

    /// <summary>
    /// Verifies that an array schema maps its item schema recursively.
    /// </summary>
    [Fact]
    public void Map_ArraySchema_ShouldMapItemsRecursively()
    {
        // Arrange
        var source =
            new AiParameterPropertyBase
            {
                Type = "array",

                Items =
                    new AiParameterPropertyBase
                    {
                        Type = "string",
                        Description =
                            "An array item."
                    }
            };

        // Act
        var result =
            _sut.Map(
                source);

        // Assert
        result.Type.Should()
            .Be(
                "array");

        result.Items.Should()
            .NotBeNull();

        result.Items.Should()
            .BeOfType<GeminiParameterProperty>();

        result.Items!.Type.Should()
            .Be(
                "string");

        result.Items.Description.Should()
            .Be(
                "An array item.");
    }

    /// <summary>
    /// Verifies that deeply nested object and collection schemas
    /// are mapped recursively.
    /// </summary>
    [Fact]
    public void Map_NestedObjectCollectionSchema_ShouldMapEntireGraphRecursively()
    {
        // Arrange
        var source =
            new AiParameterPropertyBase
            {
                Type = "object",

                Properties =
                    new Dictionary<
                        string,
                        AiParameterPropertyBase>
                    {
                        ["targets"] =
                            new()
                            {
                                Type = "array",

                                Items =
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
                                                        Type =
                                                            "integer"
                                                    },

                                                ["y"] =
                                                    new()
                                                    {
                                                        Type =
                                                            "integer"
                                                    }
                                            },

                                        Required =
                                        [
                                            "x",
                                            "y"
                                        ]
                                    }
                            }
                    },

                Required =
                [
                    "targets"
                ]
            };

        // Act
        var result =
            _sut.Map(
                source);

        // Assert
        result.Properties.Should()
            .NotBeNull();

        result.Properties.Should()
            .ContainKey(
                "targets");

        var targets =
            result.Properties!["targets"];

        targets.Should()
            .BeOfType<GeminiParameterProperty>();

        targets.Type.Should()
            .Be(
                "array");

        targets.Items.Should()
            .NotBeNull();

        targets.Items.Should()
            .BeOfType<GeminiParameterProperty>();

        var target =
            targets.Items!;

        target.Type.Should()
            .Be(
                "object");

        target.Properties.Should()
            .NotBeNull();

        target.Properties.Should()
            .ContainKey(
                "x");

        target.Properties.Should()
            .ContainKey(
                "y");

        target.Properties!["x"]
            .Should()
            .BeOfType<GeminiParameterProperty>();

        target.Properties["x"]
            .Type.Should()
            .Be(
                "integer");

        target.Properties["y"]
            .Should()
            .BeOfType<GeminiParameterProperty>();

        target.Properties["y"]
            .Type.Should()
            .Be(
                "integer");

        target.Required.Should()
            .BeEquivalentTo(
                [
                    "x",
                    "y"
                ]);
    }

    /// <summary>
    /// Verifies that mutable enumeration values are defensively copied.
    /// </summary>
    [Fact]
    public void Map_SchemaWithEnum_ShouldDefensivelyCopyEnum()
    {
        // Arrange
        var source =
            new AiParameterPropertyBase
            {
                Type = "string",

                Enum =
                [
                    "one",
                    "two"
                ]
            };

        // Act
        var result =
            _sut.Map(
                source);

        result.Enum.Should()
            .NotBeNull();

        result.Enum!.Add(
            "three");

        // Assert
        source.Enum.Should()
            .BeEquivalentTo(
                [
                    "one",
                    "two"
                ]);

        result.Enum.Should()
            .BeEquivalentTo(
                [
                    "one",
                    "two",
                    "three"
                ]);

        result.Enum.Should()
            .NotBeSameAs(
                source.Enum);
    }

    /// <summary>
    /// Verifies that mutable required property names are defensively copied.
    /// </summary>
    [Fact]
    public void Map_SchemaWithRequired_ShouldDefensivelyCopyRequired()
    {
        // Arrange
        var source =
            new AiParameterPropertyBase
            {
                Type = "object",

                Required =
                [
                    "x",
                    "y"
                ]
            };

        // Act
        var result =
            _sut.Map(
                source);

        result.Required.Should()
            .NotBeNull();

        result.Required!.Add(
            "z");

        // Assert
        source.Required.Should()
            .BeEquivalentTo(
                [
                    "x",
                    "y"
                ]);

        result.Required.Should()
            .BeEquivalentTo(
                [
                    "x",
                    "y",
                    "z"
                ]);

        result.Required.Should()
            .NotBeSameAs(
                source.Required);
    }

    /// <summary>
    /// Verifies that nested properties are mapped to new instances
    /// rather than sharing mutable schema instances with the source.
    /// </summary>
    [Fact]
    public void Map_ObjectSchema_ShouldDefensivelyCopyNestedProperties()
    {
        // Arrange
        var sourceProperty =
            new AiParameterPropertyBase
            {
                Type = "integer",
                Description = "Original description."
            };

        var source =
            new AiParameterPropertyBase
            {
                Type = "object",

                Properties =
                    new Dictionary<
                        string,
                        AiParameterPropertyBase>
                    {
                        ["x"] =
                            sourceProperty
                    }
            };

        // Act
        var result =
            _sut.Map(
                source);

        result.Properties.Should()
            .NotBeNull();

        var mappedProperty =
            result.Properties!["x"];

        mappedProperty.Description =
            "Modified description.";

        // Assert
        mappedProperty.Should()
            .NotBeSameAs(
                sourceProperty);

        sourceProperty.Description.Should()
            .Be(
                "Original description.");

        mappedProperty.Description.Should()
            .Be(
                "Modified description.");
    }

    /// <summary>
    /// Verifies that the item schema is mapped to a new instance
    /// rather than sharing mutable state with the source.
    /// </summary>
    [Fact]
    public void Map_ArraySchema_ShouldDefensivelyCopyItems()
    {
        // Arrange
        var sourceItem =
            new AiParameterPropertyBase
            {
                Type = "integer",
                Description = "Original item."
            };

        var source =
            new AiParameterPropertyBase
            {
                Type = "array",
                Items = sourceItem
            };

        // Act
        var result =
            _sut.Map(
                source);

        result.Items.Should()
            .NotBeNull();

        result.Items!.Description =
            "Modified item.";

        // Assert
        result.Items.Should()
            .NotBeSameAs(
                sourceItem);

        sourceItem.Description.Should()
            .Be(
                "Original item.");

        result.Items.Description.Should()
            .Be(
                "Modified item.");
    }

    /// <summary>
    /// Verifies that a null source is rejected before mapping begins.
    /// </summary>
    [Fact]
    public void Map_NullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        AiParameterPropertyBase source =
            null!;

        // Act
        Action act =
            () =>
                _sut.Map(
                    source);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage(
                "*source*");
    }
}