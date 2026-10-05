namespace AiUtility.ToolKits.Abstractions;

/// <summary>
/// Provides access to state owned by the current logical AI tool execution.
/// </summary>
/// <typeparam name="TState">
/// The reference type representing execution-owned state.
/// </typeparam>
public interface IAiToolExecutionStateAccessor<TState>
    where TState : class
{
    /// <summary>
    /// Gets the state owned by the current logical AI tool execution.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no AI tool execution state is active.
    /// </exception>
    TState Current { get; }

    /// <summary>
    /// Pushes execution-owned state into the current logical async flow.
    /// </summary>
    /// <param name="state">
    /// The state to associate with the current logical execution.
    /// </param>
    /// <returns>
    /// A lease that restores the previous execution state when disposed.
    /// </returns>
    IDisposable Push(TState state);
}