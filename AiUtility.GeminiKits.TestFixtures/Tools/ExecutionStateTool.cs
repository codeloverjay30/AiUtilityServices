using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.TestFixtures.Models;

namespace AiUtility.GeminiKits.TestFixtures.Tools;

/// <summary>
/// Provides a Gemini tool used to verify invocation-time instance resolution.
/// </summary>
public sealed class ExecutionStateTool
{
    private readonly ExecutionState _state;

    /// <summary>
    /// Initializes a new execution-state tool.
    /// </summary>
    /// <param name="state">
    /// The state owned by the current test execution.
    /// </param>
    public ExecutionStateTool(
        ExecutionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    /// <summary>
    /// Gets the snapshot identifier owned by this tool instance.
    /// </summary>
    /// <returns>
    /// The snapshot identifier.
    /// </returns>
    [GeminiTool(
        Description =
            "Returns the snapshot identifier owned by the current execution.")]
    public string GetSnapshotId()
    {
        return _state.SnapshotId;
    }
}