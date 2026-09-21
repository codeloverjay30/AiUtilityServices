using System.Text.Json;
using AiUtility.GeminiKits.Models;
using AiUtility.ToolKits.Models;
using FluentAssertions;

namespace AiUtility.GeminiKits.Tests;

/// <summary>
/// Verifies the JSON contract of Gemini function declaration models.
/// </summary>
public class GeminiModelJsonSerializationTests
{
    [Fact]
    public void GeminiToolDeclaration_ShouldSerializeWithGeminiFieldNames()
    {
        var declaration = new GeminiToolDeclaration
        {
            Name = "get_status",
            Description = "Gets the current status.",
            Parameters = new GeminiParameters
            {
                Type = "object",
                Properties = new Dictionary<string, GeminiParameterProperty>
                {
                    ["requestId"] = new()
                    {
                        Type = "string",
                        Description = "The request identifier."
                    }
                },
                Required = ["requestId"]
            }
        };

        string json = JsonSerializer.Serialize(declaration);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        root.EnumerateObject()
            .Select(static property => property.Name)
            .Should()
            .BeEquivalentTo(["name", "description", "parameters"]);

        root.GetProperty("name").GetString().Should().Be("get_status");
        root.GetProperty("description").GetString()
            .Should().Be("Gets the current status.");

        JsonElement parameters = root.GetProperty("parameters");

        parameters.EnumerateObject()
            .Select(static property => property.Name)
            .Should()
            .BeEquivalentTo(["type", "properties", "required"]);

        parameters.GetProperty("type").GetString().Should().Be("object");

        JsonElement requestId = parameters
            .GetProperty("properties")
            .GetProperty("requestId");

        requestId.GetProperty("type").GetString().Should().Be("string");
        requestId.GetProperty("description").GetString()
            .Should().Be("The request identifier.");

        parameters.GetProperty("required")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Should()
            .Equal("requestId");
    }

    [Fact]
    public void GeminiToolDeclaration_ShouldRestoreValuesAfterJsonRoundTrip()
    {
        const string json = """
            {
              "name": "get_status",
              "description": "Gets the current status.",
              "parameters": {
                "type": "object",
                "properties": {
                  "requestId": {
                    "type": "string",
                    "description": "The request identifier."
                  }
                },
                "required": ["requestId"]
              }
            }
            """;

        GeminiToolDeclaration? declaration =
            JsonSerializer.Deserialize<GeminiToolDeclaration>(json);

        declaration.Should().NotBeNull();

        declaration!.Name.Should().Be("get_status");
        declaration.Description.Should().Be("Gets the current status.");
        declaration.Parameters.Type.Should().Be("object");
        declaration.Parameters.Required.Should().Equal("requestId");

        declaration.Parameters.Properties
            .Should().ContainKey("requestId");

        declaration.Parameters.Properties["requestId"].Type
            .Should().Be("string");

        declaration.Parameters.Properties["requestId"].Description
            .Should().Be("The request identifier.");

        AiToolDeclarationBase baseDeclaration = declaration;
        baseDeclaration.Name.Should().Be("get_status");
        baseDeclaration.Description.Should().Be("Gets the current status.");

        baseDeclaration.Parameters
            .Should().BeOfType<GeminiParameters>();

        AiParametersBase baseParameters = declaration.Parameters;
        baseParameters.Type.Should().Be("object");
        baseParameters.Required.Should().Equal("requestId");

        baseParameters.Properties["requestId"]
            .Should().BeOfType<GeminiParameterProperty>();
    }

    [Fact]
    public void GeminiToolDeclaration_ShouldPreserveNestedSchemaWithoutBasePropertyNames()
    {
        var itemSchema = new GeminiParameterProperty
        {
            Type = "string",
            Description = "A status value.",
            Enum = ["pending", "completed"]
        };

        var statusListSchema = new GeminiParameterProperty
        {
            Type = "array",
            Description = "The status values."
        };

        ((AiParameterPropertyBase)statusListSchema).Items = itemSchema;

        var filterSchema = new GeminiParameterProperty
        {
            Type = "object",
            Description = "The filter criteria."
        };

        AiParameterPropertyBase baseFilterSchema = filterSchema;

        baseFilterSchema.Properties =
            new Dictionary<string, AiParameterPropertyBase>
            {
                ["statuses"] = statusListSchema
            };

        baseFilterSchema.Required = ["statuses"];

        var declaration = new GeminiToolDeclaration
        {
            Name = "get_status",
            Description = "Gets matching statuses.",
            Parameters = new GeminiParameters
            {
                Type = "object",
                Properties = new Dictionary<string, GeminiParameterProperty>
                {
                    ["filter"] = filterSchema
                },
                Required = ["filter"]
            }
        };

        string json = JsonSerializer.Serialize(declaration);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        root.EnumerateObject()
            .Select(static property => property.Name)
            .Should()
            .BeEquivalentTo(["name", "description", "parameters"]);

        JsonElement parameters = root.GetProperty("parameters");

        parameters.EnumerateObject()
            .Select(static property => property.Name)
            .Should()
            .BeEquivalentTo(["type", "properties", "required"]);

        JsonElement filter = parameters
            .GetProperty("properties")
            .GetProperty("filter");

        filter.EnumerateObject()
            .Select(static property => property.Name)
            .Should()
            .BeEquivalentTo(
                ["type", "description", "properties", "required"]);

        filter.GetProperty("type").GetString()
            .Should().Be("object");

        filter.GetProperty("description").GetString()
            .Should().Be("The filter criteria.");

        filter.GetProperty("required")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Should()
            .Equal("statuses");

        JsonElement statuses = filter
            .GetProperty("properties")
            .GetProperty("statuses");

        statuses.EnumerateObject()
            .Select(static property => property.Name)
            .Should()
            .BeEquivalentTo(["type", "description", "items"]);

        statuses.GetProperty("type").GetString()
            .Should().Be("array");

        JsonElement items = statuses.GetProperty("items");

        items.EnumerateObject()
            .Select(static property => property.Name)
            .Should()
            .BeEquivalentTo(["type", "description", "enum"]);

        items.GetProperty("type").GetString()
            .Should().Be("string");

        items.GetProperty("description").GetString()
            .Should().Be("A status value.");

        items.GetProperty("enum")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Should()
            .Equal("pending", "completed");

        GeminiToolDeclaration? restored =
            JsonSerializer.Deserialize<GeminiToolDeclaration>(json);

        restored.Should().NotBeNull();

        AiToolDeclarationBase restoredBaseDeclaration = restored!;
        restoredBaseDeclaration.Parameters
            .Should().BeOfType<GeminiParameters>();

        AiParametersBase restoredBaseParameters = restored.Parameters;

        restoredBaseParameters.Properties["filter"]
            .Should().BeOfType<GeminiParameterProperty>();

        AiParameterPropertyBase restoredFilter =
            restoredBaseParameters.Properties["filter"];

        restoredFilter.Properties.Should().NotBeNull();
        restoredFilter.Properties!.Should().ContainKey("statuses");
        restoredFilter.Required.Should().Equal("statuses");

        AiParameterPropertyBase restoredStatuses =
            restoredFilter.Properties["statuses"];

        restoredStatuses.Items
            .Should().BeOfType<GeminiParameterProperty>();

        restoredStatuses.Items!.Type.Should().Be("string");
        restoredStatuses.Items.Enum
            .Should().Equal("pending", "completed");
    }

}