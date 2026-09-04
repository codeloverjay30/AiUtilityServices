using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Attributes;
using AiUtility.ToolKits.Executor;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Executor;

/// <summary>
/// Provides Gemini-specific tool execution by using the shared AI tool execution pipeline.
/// </summary>
public sealed class GeminiToolExecutor
    : AiToolExecutor<GeminiToolMetadata, GeminiToolAttribute>,
      IGeminiToolExecutor
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GeminiToolExecutor"/> class.
    /// </summary>
    /// <param name="registry">
    /// The Gemini tool registry.
    /// </param>
    /// <param name="typeUtilityService">
    /// The type conversion utility service.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required dependency is null.
    /// </exception>
    public GeminiToolExecutor(
        IGeminiToolRegistry registry,
        ITypeUtilityService typeUtilityService)
        : base(
            registry
            ?? throw new ArgumentNullException(nameof(registry)),
            typeUtilityService
            ?? throw new ArgumentNullException(nameof(typeUtilityService)))
    {
    }
}