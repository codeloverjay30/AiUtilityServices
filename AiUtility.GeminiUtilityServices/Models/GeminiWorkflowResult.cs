using CommonModels;


/// <summary>
/// Represents the result of a Gemini agent workflow.
/// </summary>
public sealed class GeminiWorkflowResult
{
    /// <summary>
    /// Gets or initializes the workflow completion status.
    /// </summary>
    public WorkflowCompletionStatus WorkflowStatus { get; init; }

    /// <summary>
    /// Gets or initializes the business task execution status.
    /// </summary>
    public TaskExecutionStatus TaskStatus { get; init; }

    /// <summary>
    /// Gets or initializes the final message returned by Gemini.
    /// </summary>
    public string FinalMessage { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes detailed execution statuses produced by the workflow.
    /// </summary>
    public StatusJsonModels ExecutionDetails { get; init; } = new();
}