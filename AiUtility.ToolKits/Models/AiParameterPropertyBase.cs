namespace AiUtility.ToolKits.Models;

/// <summary>
/// Represents a provider-independent AI parameter schema property.
/// </summary>
public class AiParameterPropertyBase
{
    /// <summary>
    /// Gets or sets the schema type.
    /// </summary>
    public string Type { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets or sets the parameter description.
    /// </summary>
    public string Description { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets or sets the allowed enumeration values.
    /// </summary>
    public List<string>? Enum { get; set; }

    /// <summary>
    /// Gets or sets nested object properties.
    /// </summary>
    public Dictionary<string, AiParameterPropertyBase>?
        Properties
    { get; set; }

    /// <summary>
    /// Gets or sets required nested property names.
    /// </summary>
    public List<string>? Required { get; set; }

    /// <summary>
    /// Gets or sets the element schema when this property
    /// represents a collection.
    /// </summary>
    public AiParameterPropertyBase?
        Items
    { get; set; }
}