using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.AiBaseUtilityServices.Models
{
    /// <summary>
    /// Info of progress bar used on workflow
    /// </summary>
    public class WorkflowProgress
    {
        /// <summary>
        /// percentage
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// current step (i.e. nth attempts in the same task execution) 
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// max available steps. For more details, see <seealso cref="global::AiUtility.GeminiUtilityServices.Models.AiExecutionSettings.MaxStep"/> property.
        /// </summary>
        public int MaxSteps { get; set; }

        /// <summary>
        /// Description of current action
        /// </summary>
        public string CurrentAction { get; set; } = string.Empty;

        /// <summary>
        /// Metadata used for execution status model <seealso cref="global::Models.StatusJsonModel.Metadata"/>
        /// </summary>
        public Dictionary<string , string> Metadata { get; set; } = new();

        /// <summary>
        /// The default format
        /// </summary>
        public virtual string Formatting => "[{0}%] Step {1}/{2}: {3}";
        public override string ToString() => string.Format(Formatting , Percentage , CurrentStep , MaxSteps);
    }
}
