using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Security;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Services;

/// <summary>
/// Dispatches registered Gemini tools after enforcing execution authorization.
/// </summary>
public class GeminiToolDispatcher : IGeminiToolDispatcher
{
    private readonly IGeminiToolRegistry _toolRegistry;
    private readonly ITypeUtilityService _typeUtilityService;
    private readonly GeminiToolExecutionPolicy _policy;

    public IGeminiToolRegistry ToolRegistry => _toolRegistry;
    public ITypeUtilityService TypeUtilityService => _typeUtilityService;

    /// <summary>
    /// Initializes the dispatcher with an optional explicit execution policy.
    /// </summary>
    public GeminiToolDispatcher(
        IGeminiToolRegistry toolRegistry,
        ITypeUtilityService typeUtilityService,
        GeminiToolExecutionPolicy? policy = null)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _typeUtilityService = typeUtilityService ?? throw new ArgumentNullException(nameof(typeUtilityService));
        _policy = policy ?? GeminiToolExecutionPolicy.AllowRegisteredTools();
    }

    /// <summary>
    /// Dispatches a registered and authorized Gemini tool.
    /// </summary>
    public async Task<object?> DispatchAsync(
        string functionName,
        Dictionary<string, object> arguments,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentNullException.ThrowIfNull(arguments);
        ct.ThrowIfCancellationRequested();

        if (!_toolRegistry.TryGetTool(functionName, out var metadata) || metadata is null)
        {
            throw new KeyNotFoundException($"[Dispatcher] 找不到名稱為 '{functionName}' 的工具。");
        }

        _policy.EnsureAllowed(metadata.MethodInfo);
        var parameters = metadata.Parameters;
        object?[] paramValues = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (param.ParameterType == typeof(CancellationToken))
            {
                paramValues[i] = ct;
                continue;
            }

            object? value;
            if (arguments.TryGetValue(param.Name!, out var supplied))
            {
                value = _typeUtilityService.SafeConvert(supplied, param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                value = param.DefaultValue;
            }
            else
            {
                throw new ArgumentException($"Missing required argument: {param.Name}", nameof(arguments));
            }

            foreach (var attribute in param.GetCustomAttributes<ValidationAttribute>())
            {
                attribute.Validate(value, param.Name ?? "Parameter");
            }

            paramValues[i] = value;
        }

        object? instance = null;
        if (!metadata.MethodInfo.IsStatic)
        {
            instance = metadata.InstanceFactory?.Invoke()
                ?? throw new InvalidOperationException(
                    $"Instance factory is unavailable for tool '{metadata.FunctionName}'.");
        }

        var result = metadata.FastInvoke(instance, paramValues);
        if (result is not Task task)
        {
            return result;
        }

        await task.WaitAsync(ct).ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }
}
