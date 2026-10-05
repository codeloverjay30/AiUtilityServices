using AiUtility.ToolKits.Execution;

namespace AiUtility.ToolKits.Models;

    /// <summary>
    /// Restores the previous execution state when the lease is disposed.
    /// </summary>
    public sealed class ExecutionStateLease<TState>
        : IDisposable
        where TState : class
    {
        private AiToolExecutionStateAccessor<TState>? _owner;
        private StateFrame<TState>? _frame;

        /// <summary>
        /// Initializes a new execution-state lease.
        /// </summary>
        /// <param name="owner">
        /// The accessor that owns the execution frame.
        /// </param>
        /// <param name="frame">
        /// The execution frame represented by this lease.
        /// </param>
        public ExecutionStateLease(
            AiToolExecutionStateAccessor<TState> owner,
            StateFrame<TState> frame)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(frame);

            _owner = owner;
            _frame = frame;
        }

        /// <summary>
        /// Restores the previous execution state.
        /// </summary>
        public void Dispose()
        {
            AiToolExecutionStateAccessor<TState>? owner =
                _owner;

            StateFrame<TState>? frame =
                _frame;

            if (owner is null ||
                frame is null)
            {
                return;
            }

            owner.Pop(frame);

            _owner = null;
            _frame = null;
        }
    }
