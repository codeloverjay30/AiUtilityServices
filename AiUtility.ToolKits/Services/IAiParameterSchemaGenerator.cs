using AiUtility.ToolKits.Models;

namespace AiUtility.ToolKits.Services;

/// <summary>
/// Defines a service for generating provider-independent parameter schemas
/// from CLR types.
/// </summary>
public interface IAiParameterSchemaGenerator
{
    /// <summary>
    /// Generates a parameter schema for the specified CLR type.
    /// </summary>
    /// <typeparam name="T">
    /// The CLR type to generate a schema for.
    /// </typeparam>
    /// <returns>
    /// The generated parameter schema.
    /// </returns>
    AiParameterPropertyBase Generate<T>();

    /// <summary>
    /// Generates a parameter schema for the specified CLR type.
    /// </summary>
    /// <param name="type">
    /// The CLR type to generate a schema for.
    /// </param>
    /// <returns>
    /// The generated parameter schema.
    /// </returns>
    AiParameterPropertyBase Generate(
        Type type);
}