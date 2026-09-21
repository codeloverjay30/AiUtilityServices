/// <summary>
/// Represents the business outcome of the requested task.
/// </summary>
public enum TaskExecutionStatus
{
    /// <summary>
    /// The task outcome has not been determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// The requested task was completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The requested task completed without achieving its goal.
    /// </summary>
    Failed
}