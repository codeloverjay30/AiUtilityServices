using AiUtility.AiBaseUtilityServices.Consts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AiUtility.AiBaseUtilityServices.Models
{
    /// <summary>
    /// settings for execution
    /// </summary>
    public class AiExecutionSettings
    {
        /// <summary>
        /// The last n tokens to keep when consolidating the token.
        /// </summary>
        [Range(1 , int.MaxValue , ErrorMessage = Constants.Constraints.ValueConstraints.VALUE_MUST_BE_POSITIVE)]
        public int LastTokenCountNeededToBeKept { get; set; }

        /// <summary>
        /// Max steps in one task by automatically engine (<seealso cref="AiUtility.GeminiUtilityServices.Services.GeminiSessionManager.ExecuteAutomationStepAsync(GeminiGenerateRequest, string, AiExecutionSettings, CancellationToken)"/>
        /// </summary>
        [Range(0 , int.MaxValue , ErrorMessage = Constants.Constraints.ValueConstraints.VALUE_MUST_BE_NONNEGATIVE)]
        public int MaxSteps { get; set; } = Constants.ExecutionSettings.MAX_STEPS;

        /// <summary>
        /// The max token as threshold for automatically engine  (<seealso cref="AiUtility.GeminiUtilityServices.Services.GeminiSessionManager.ExecuteAutomationStepAsync(GeminiGenerateRequest, string, AiExecutionSettings, CancellationToken)"/>
        /// </summary>
        [Range(0 , int.MaxValue , ErrorMessage = Constants.Constraints.ValueConstraints.VALUE_MUST_BE_NONNEGATIVE)]
        public int Threshold { get; set; } = Constants.ExecutionSettings.MAX_THRESHOLD;

        [Range(0 , int.MaxValue , ErrorMessage = Constants.Constraints.ValueConstraints.TIMEOUT_MUST_BE_NONNEGATIVE)]
        public TimeSpan ToolExecutionTimeout { get; set; } = Constants.Timeouts.DEFAULT_TOOL_EXECUTION_TIMEOUTS;
        /// <summary>
        /// To determine to auto-execute the tool sequentially, or not.
        /// </summary>
        public bool ForceSequentialToolExecution { get; set; } = false;

        /// <summary>
        /// metadata used for execution, then it might be assigned to <see cref="global::Models.StatusJsonModel.Metadata"/>
        /// </summary>
        public Dictionary<string , string> Metadata { get; set; } = new();
    }
}
