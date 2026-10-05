namespace AiUtility.GeminiKits.TestFixtures.Models;

/// <summary>
/// Represents execution-specific state used by Gemini tool integration tests.
/// </summary>
/// <param name="SnapshotId">
/// The snapshot identifier associated with the execution.
/// </param>
public sealed record ExecutionState(
    string SnapshotId);
