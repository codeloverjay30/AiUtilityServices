/// <summary>
/// Represents the completion state of an AI workflow.
/// </summary>
public enum WorkflowCompletionStatus
{
    /// <summary>
    /// The workflow has not completed yet.
    /// </summary>
    InProgress,

    /// <summary>
    /// The workflow completed normally.
    /// </summary>
    Completed,

    /// <summary>
    /// The workflow failed because of an infrastructure or protocol error.
    /// </summary>
    Failed,

    /// <summary>
    /// The workflow was cancelled.
    /// </summary>
    Cancelled
}