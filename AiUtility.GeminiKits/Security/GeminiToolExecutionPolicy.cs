using System.Reflection;

namespace AiUtility.GeminiKits.Security;

/// <summary>
/// Enforces an immutable method-identity allowlist for Gemini tool execution.
/// </summary>
public sealed class GeminiToolExecutionPolicy
{
    private readonly HashSet<MethodInfo>? _allowedMethods;

    private GeminiToolExecutionPolicy(HashSet<MethodInfo>? allowedMethods)
    {
        _allowedMethods = allowedMethods;
    }

    /// <summary>
    /// Creates a compatibility policy that permits registered tools.
    /// This policy must not be used for Protected Mode.
    /// </summary>
    public static GeminiToolExecutionPolicy AllowRegisteredTools() => new(null);

    /// <summary>
    /// Creates a deny-by-default policy allowing only the supplied method identities.
    /// </summary>
    public static GeminiToolExecutionPolicy Protected(params MethodInfo[] allowedMethods)
    {
        ArgumentNullException.ThrowIfNull(allowedMethods);
        if (allowedMethods.Any(static method => method is null))
        {
            throw new ArgumentException("Allowed methods cannot contain null.", nameof(allowedMethods));
        }

        return new GeminiToolExecutionPolicy(new HashSet<MethodInfo>(allowedMethods));
    }

    /// <summary>
    /// Rejects a tool whose actual registered method is not authorized.
    /// </summary>
    public void EnsureAllowed(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        if (_allowedMethods is not null && !_allowedMethods.Contains(method))
        {
            throw new InvalidOperationException(
                $"Gemini tool execution denied by Protected policy: '{method.DeclaringType?.FullName}.{method.Name}'.");
        }
    }
}
