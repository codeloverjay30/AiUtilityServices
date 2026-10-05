using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.TestFixtures.Models;

namespace AiUtility.GeminiKits.TestFixtures.Tools;

/// <summary>
/// Provides a Gemini tool whose result is bound to execution-specific state.
/// </summary>
public sealed class StatefulExecutionTool
{
    private readonly ExecutionState _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatefulExecutionTool"/> class.
    /// </summary>
    /// <param name="state">
    /// The state owned by the execution that creates this tool instance.
    /// </param>
    public StatefulExecutionTool(
        ExecutionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        _state = state;
    }

    /// <summary>
    /// Gets the snapshot identifier owned by this tool instance.
    /// </summary>
    /// <returns>
    /// The snapshot identifier associated with the execution state.
    /// </returns>
    [GeminiTool(
        Description =
            "Returns the snapshot identifier owned by the current execution.")]
    public string GetSnapshotId()
    {
        return _state.SnapshotId;
    }
}