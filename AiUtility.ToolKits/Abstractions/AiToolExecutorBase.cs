using AiUtility.ToolKits.Abstractions;
using TypeUtilityServices;

namespace AiUtility.ToolKits.Executor;

/// <summary>
/// Provides the common execution pipeline for registered AI tools.
/// </summary>
/// <typeparam name="TMetadata">
/// The metadata type used to describe a registered tool.
/// </typeparam>
/// <typeparam name="TAttribute">
/// The attribute type used to identify registered tools.
/// </typeparam>
public abstract class AiToolExecutorBase<TMetadata, TAttribute>
    : IAiToolExecutor<TMetadata, TAttribute>
    where TMetadata : ToolMetadataBase
    where TAttribute : Attribute
{
    private readonly IToolRegistry<TMetadata, TAttribute> _registry;
    private readonly ITypeUtilityService _typeUtilityService;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AiToolExecutorBase{TMetadata, TAttribute}"/> class.
    /// </summary>
    /// <param name="registry">
    /// The registry used to resolve tool metadata.
    /// </param>
    /// <param name="typeUtilityService">
    /// The utility service used to convert supplied arguments to target parameter types.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required dependency is null.
    /// </exception>
    protected AiToolExecutorBase(
        IToolRegistry<TMetadata, TAttribute> registry,
        ITypeUtilityService typeUtilityService)
    {
        _registry =
            registry
            ?? throw new ArgumentNullException(nameof(registry));

        _typeUtilityService =
            typeUtilityService
            ?? throw new ArgumentNullException(nameof(typeUtilityService));
    }

    /// <summary>
    /// Executes a registered tool asynchronously.
    /// </summary>
    /// <param name="functionName">
    /// The registered tool function name.
    /// </param>
    /// <param name="arguments">
    /// The arguments supplied to the tool.
    /// </param>
    /// <param name="ct">
    /// The cancellation token used for asynchronous execution.
    /// </param>
    /// <returns>
    /// The value returned by the invoked tool, or <see langword="null"/>
    /// when the tool has no return value.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="functionName"/> is null, empty, or whitespace,
    /// or when a required tool argument is missing.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="arguments"/> is null.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the requested tool is not registered.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when cancellation is requested.
    /// </exception>
    public virtual async Task<object?> ExecuteAsync(
        string functionName,
        IDictionary<string, object> arguments,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentNullException.ThrowIfNull(arguments);

        ct.ThrowIfCancellationRequested();

        if (!_registry.TryGetTool(functionName, out var metadata) ||
            metadata is null)
        {
            throw new KeyNotFoundException(
                $"Tool '{functionName}' was not found.");
        }

        var invokeArgs =
            PrepareArgs(
                metadata,
                arguments,
                ct);

        var instance =
            metadata.InstanceFactory?.Invoke();

        var result =
            metadata.FastInvoke(
                instance,
                invokeArgs);

        return await UnwrapResultAsync(
                result,
                ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Prepares invocation arguments for the specified tool metadata.
    /// </summary>
    /// <param name="metadata">
    /// The metadata describing the target tool.
    /// </param>
    /// <param name="arguments">
    /// The supplied tool arguments.
    /// </param>
    /// <param name="ct">
    /// The cancellation token to inject into matching parameters.
    /// </param>
    /// <returns>
    /// An argument array suitable for invoking the target method.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required argument is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a required method argument is missing.
    /// </exception>
    protected virtual object?[] PrepareArgs(
        TMetadata metadata,
        IDictionary<string, object> arguments,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(arguments);

        var parameters =
            metadata.MethodInfo.GetParameters();

        var invokeArgs =
            new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter =
                parameters[i];

            if (parameter.ParameterType ==
                typeof(CancellationToken))
            {
                invokeArgs[i] = ct;
                continue;
            }

            if (arguments.TryGetValue(
                    parameter.Name!,
                    out var value))
            {
                invokeArgs[i] =
                    _typeUtilityService.SafeConvert(
                        value,
                        parameter.ParameterType);

                continue;
            }

            if (parameter.HasDefaultValue)
            {
                invokeArgs[i] =
                    parameter.DefaultValue;

                continue;
            }

            throw new ArgumentException(
                $"Missing required argument: {parameter.Name}",
                nameof(arguments));
        }

        return invokeArgs;
    }

    /// <summary>
    /// Unwraps synchronous, <see cref="Task"/>, and generic task results.
    /// </summary>
    /// <param name="result">
    /// The raw invocation result.
    /// </param>
    /// <param name="ct">
    /// The cancellation token used while awaiting the result.
    /// </param>
    /// <returns>
    /// The unwrapped result value.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when cancellation is requested.
    /// </exception>
    protected virtual async Task<object?> UnwrapResultAsync(
        object? result,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (result is not Task task)
        {
            return result;
        }

        await task
            .WaitAsync(ct)
            .ConfigureAwait(false);

        var taskType =
            task.GetType();

        if (!taskType.IsGenericType)
        {
            return null;
        }

        return taskType
            .GetProperty(nameof(Task<object>.Result))?
            .GetValue(task);
    }
}