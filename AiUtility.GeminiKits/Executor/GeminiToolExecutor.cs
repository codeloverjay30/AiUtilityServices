using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Security;
using AiUtility.ToolKits.Executor;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Executor;

/// <summary>
/// Executes Gemini tools with an independent authorization check.
/// </summary>
public sealed class GeminiToolExecutor
    : AiToolExecutor<GeminiToolMetadata, GeminiToolAttribute>, IGeminiToolExecutor
{
    private readonly IGeminiToolRegistry _registry;
    private readonly GeminiToolExecutionPolicy _policy;

    /// <summary>
    /// Initializes the executor with an optional explicit execution policy.
    /// </summary>
    public GeminiToolExecutor(
        IGeminiToolRegistry registry,
        ITypeUtilityService typeUtilityService,
        GeminiToolExecutionPolicy? policy = null)
        : base(registry ?? throw new ArgumentNullException(nameof(registry)),
               typeUtilityService ?? throw new ArgumentNullException(nameof(typeUtilityService)))
    {
        _registry = registry;
        _policy = policy ?? GeminiToolExecutionPolicy.AllowRegisteredTools();
    }

    /// <summary>
    /// Authorizes the resolved method before invoking the shared execution pipeline.
    /// </summary>
    public override Task<object?> ExecuteAsync(
        string functionName,
        IDictionary<string, object> arguments,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentNullException.ThrowIfNull(arguments);
        ct.ThrowIfCancellationRequested();

        if (!_registry.TryGetTool(functionName, out var metadata) || metadata is null)
        {
            throw new KeyNotFoundException($"Tool '{functionName}' was not found.");
        }

        _policy.EnsureAllowed(metadata.MethodInfo);
        return base.ExecuteAsync(functionName, arguments, ct);
    }
}
