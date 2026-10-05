namespace AiUtility.ToolKits.Models;

    /// <summary>
    /// Represents an immutable frame in the execution-state stack.
    /// </summary>
    public sealed class StateFrame<TState>
        where TState : class
    {
        /// <summary>
        /// Initializes a new execution-state frame.
        /// </summary>
        /// <param name="state">
        /// The state owned by this execution frame.
        /// </param>
        /// <param name="previous">
        /// The preceding execution frame, if any.
        /// </param>
        public StateFrame(
            TState state,
            StateFrame<TState>? previous)
        {
            ArgumentNullException.ThrowIfNull(state);

            State = state;
            Previous = previous;
        }

        /// <summary>
        /// Gets the state owned by this frame.
        /// </summary>
        public TState State { get; }

        /// <summary>
        /// Gets the preceding execution frame.
        /// </summary>
        public StateFrame<TState>? Previous { get; }
    }
