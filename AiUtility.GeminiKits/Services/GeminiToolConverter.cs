using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Mappers;
using AiUtility.GeminiKits.Models;
using AiUtility.ToolKits.Consts;
using AiUtility.ToolKits.Services;
using EnumUtilityServices;
using JsonUtilityServices;
using System.ComponentModel;
using System.Reflection;

namespace AiUtility.GeminiKits.Services;

/// <summary>
/// Converts registered tool metadata into Gemini-compatible tool declarations.
/// </summary>
public class GeminiToolConverter(
    IJsonUtilityService jsonUtilityService,
    IEnumUtilityService enumUtilityService,
    IAiParameterSchemaGenerator parameterSchemaGenerator,
    IGeminiParameterPropertyMapper parameterPropertyMapper,
    string defaultDescription =
        AiToolConstants.DefaultDescription,
    string defaultParameterDescription =
        AiToolConstants.DefaultParameterDescription)
    : AiToolConverterBase<
        GeminiToolAttribute,
        GeminiToolDeclaration,
        GeminiParameters,
        GeminiParameterProperty>(
            jsonUtilityService,
            enumUtilityService,
            defaultDescription,
            defaultParameterDescription)
{
    private readonly IAiParameterSchemaGenerator
        _parameterSchemaGenerator =
            parameterSchemaGenerator
            ?? throw new ArgumentNullException(
                nameof(parameterSchemaGenerator));

    private readonly IGeminiParameterPropertyMapper
        _parameterPropertyMapper =
            parameterPropertyMapper
            ?? throw new ArgumentNullException(
                nameof(parameterPropertyMapper));

    /// <summary>
    /// Gets the tool description from the Gemini tool attribute.
    /// </summary>
    /// <param name="attr">
    /// The Gemini tool attribute.
    /// </param>
    /// <returns>
    /// The configured tool description, or <see langword="null"/>.
    /// </returns>
    protected override string? GetDescriptionFromAttribute(
        GeminiToolAttribute? attr)
    {
        return attr?.Description;
    }

    /// <summary>
    /// Creates a Gemini-compatible schema property for the specified
    /// tool parameter.
    /// </summary>
    /// <param name="parameter">
    /// The tool parameter metadata.
    /// </param>
    /// <returns>
    /// The generated Gemini parameter schema.
    /// </returns>
    protected override GeminiParameterProperty CreateParameterProperty(
        ParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(
            parameter);

        var schema =
            _parameterSchemaGenerator.Generate(
                parameter.ParameterType);

        var property =
            _parameterPropertyMapper.Map(
                schema);

        property.Description =
            parameter
                .GetCustomAttribute<DescriptionAttribute>()
                ?.Description
            ?? DefaultParameterDescription;

        return property;
    }

}