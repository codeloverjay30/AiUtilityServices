using AiUtility.ToolKits.Abstractions;
using AiUtility.ToolKits.Models;
using System.Threading;

namespace AiUtility.ToolKits.Execution;

/// <summary>
/// Maintains execution-owned state within the current logical asynchronous flow.
/// </summary>
/// <typeparam name="TState">
/// The reference type representing execution-owned state.
/// </typeparam>
public sealed class AiToolExecutionStateAccessor<TState>
    : IAiToolExecutionStateAccessor<TState>
    where TState : class
{
    private const string NoActiveStateMessage =
        "No active AI tool execution state is available.";

    private readonly AsyncLocal<StateFrame<TState>?> _currentFrame = new();

    /// <inheritdoc />
    public TState Current =>
        _currentFrame.Value?.State
        ?? throw new InvalidOperationException(
            NoActiveStateMessage);

    /// <inheritdoc />
    public IDisposable Push(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        StateFrame<TState>? previousFrame =
            _currentFrame.Value;

        StateFrame<TState> currentFrame =
            new(
                state,
                previousFrame);

        _currentFrame.Value =
            currentFrame;

        return new ExecutionStateLease<TState>(
            this,
            currentFrame);
    }

    /// <summary>
    /// Restores the state that preceded the specified execution frame.
    /// </summary>
    /// <param name="frame">
    /// The execution frame being released.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when execution state leases are disposed out of order.
    /// </exception>
    public void Pop(StateFrame<TState> frame)
    {
        if (!ReferenceEquals(
                _currentFrame.Value,
                frame))
        {
            throw new InvalidOperationException(
                "AI tool execution state leases must be disposed in reverse order.");
        }

        _currentFrame.Value =
            frame.Previous;
    }
}