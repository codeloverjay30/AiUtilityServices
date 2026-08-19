using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AiUtility.AiBaseUtilityServices.Consts
{
    public static partial class Constants
    {
        public static class Vocabulary
        {
            public const string PREPARE = "prepare";
            public const string SEND = "send";
            public const string MODEL = "model";
            public const string MANAGE = "manage";
            public const string API = "API";
            public const string NONNEGATIVE = "nonnegative";
            public const string POSITIVE = "positive";
            public const string VALUE = "value";
            public const string TIMEOUT = "timeout";
            public const string IMAGE = "image";
            public const string FORMAT = "format";
            public const string MEMORY = "memory";
            public const string DISK = "disk";
            public const string SPACE = "space";
            public const string MILESTONE = "milestone";
            public const string SUMMARY = "summary";
            public const string SUMMARIZE = "summarize";
            public const string ENVIRONMENT = "environment";
            public const string REVIEW = "review";
            public const string UNSUPPORTED = "unsupported";
            public const string PARSE = "parse";
            public const string EXCEPTION = "exception";
        }

        public static class Protocols
        {
            public static class Network
            {
                public const string HTTP = "http";
                public const string HTTPS = "https";
                public const string DEFAULT = HTTP;
            }
        }
        public static class Timeouts
        {
            public static readonly TimeSpan DEFAULT_TIMEOUTS = TimeSpan.FromMinutes(1); // Defauolt Timeout is 1 min
            public static readonly TimeSpan DEFAULT_TOOL_EXECUTION_TIMEOUTS = TimeSpan.FromSeconds(30); // Default Timeout is 30 sec
        }
        public static class ExecutionSettings
        {
            public const int MAX_STEPS = 10;
            public const int MAX_THRESHOLD = 3000000;
            public const int AVAILABLE_MAX_TOKENS = 8196;
            public const int DEFAULT_MAX_TOKENS = 2048;

            [Range(double.Epsilon, AVAILABLE_MAX_TEMPERATURE + double.Epsilon , ErrorMessage = Constraints.ValueConstraints.TEMPERATURE_MUST_BETWEEN_ZERO_AND_TWO)]
            public const double DEFAULT_TEMPERATURE = 0.7;
            public const double AVAILABLE_MAX_TEMPERATURE = 2.0;

        }

        public static class ProgressBars
        {
            public const int BASE_OFFSET_PERCENTAGE = 2; // 2%
            public const int COMPLETED_PERCENTAGE = 100; // 100%, indicating the task is completed
        }
        public static class AiModels
        {
            public const string AI = "AI";
            public const string AI_MODEL = $"{AI} {Vocabulary.MODEL}";

            public const string GEMINI = "Gemini";
            public const string GEMINI_API = $"{GEMINI} {Vocabulary.API}";
            public const string GEMINI_AI = $"{GEMINI} {AI}";
            public const string GEMINI_AI_MODEL = $"{GEMINI_AI} {Vocabulary.MODEL}";

            public const string PROMPT = "prompt";
            public const string CONTENT = "content";
            public const string REQUEST = "request";
            public const string TOKEN = "token";
            public const string MAX_OUTPUT_TOKEN = "MaxOutputToken";
            public const string TEMPERATURE = "temperature";
        }
        public static class ToolTasks
        {
            public const string TASK = "task";
            public const string PREPARE_TO_SEND_PROMPT_TO_AI_MODEL = $"{Vocabulary.PREPARE} to {SEND_PROMPT_TO_AI_MODEL}";
            public const string SEND_PROMPT_TO_AI_MODEL = $"{Vocabulary.SEND} {AiModels.PROMPT} to {AiModels.AI_MODEL}"; 
            public const string AI_THINKING = $"{AiModels.AI} thinking...";
            public const string AI_EXECUTING_TASK = $"{AiModels.AI} is {ExecutionStatus.EXECUTING} {TASK}: {{0}}";
            public const string EXECUTING_TASK = $"{ExecutionStatus.EXECUTING} {TASK}: {{0}}";
            public const string PREPARE_TO_EXECUTE_TASK = $"{Vocabulary.PREPARE} to {ExecutionStatus.EXECUTE} the {TASK}...";
            public const string TASK_IS_CANCELLED = $"{TASK} is {ExecutionStatus.CANCELLED}";
            public const string TASK_IS_CANCELLED_OR_ENCOUNTERS_TIMEOUT = $"{TASK} is {ExecutionStatus.CANCELLED} or encounters {Vocabulary.TIMEOUT}";
        }

        public static class AiTasks
        {
            public static class Consolidations
            {
                public const string PURGE_OLD_MEDIA_TO_SAVE_TOKEN_SPACE = $"[purge old media to save the {Vocabulary.SPACE} for more available {AiModels.TOKEN}s used for {AiModels.AI} {Vocabulary.API}]";
                public const string SUMMARIZE_MILESTONE_TO_SAVE_SPACE = $"please {Vocabulary.SUMMARIZE} the {Vocabulary.MILESTONE} and current {Vocabulary.ENVIRONMENT}to save {Vocabulary.SPACE} for more available {AiModels.TOKEN}s used for {AiModels.AI} {Vocabulary.API}";
            }

            public static class Remembers
            {
                public const string REVIEW_TASKS_AND_MILESTONE = $"Take {Vocabulary.REVIEW} of {ExecutionStatus.EXECUTED} {ToolTasks.TASK} and {Vocabulary.MILESTONE}";
                public const string REVIEW_TASKS_AND_MILESTONE_FORMAT = $"[{REVIEW_TASKS_AND_MILESTONE}]:{{0}}";
            }
        }

        public static class AiApi
        {
            public static class GeminiAiStudio
            {
                public static class AiSchema
                {
                    public static class Roles
                    {
                        public const string USER = "user";
                        public const string ROLE = "role";
                        public const string MODEL = "model";
                    }

                    public static class FunctionCall
                    {
                        public const string FUNCTION = "function";
                    }

                    public static class FunctionParameters
                    {
                        public const string TYPE = "type";
                        public const string PROPERTIES = "properties";
                        public const string REQUIRED = "required";
                        public const string ITEMS = "items";

                    }

                    /// <summary>
                    /// Safety setting used for Gemini AI Studio, see <seealso cref="AiUtility.GeminiUtilityServices.Models.GeminiSafetySettings"/>
                    /// </summary>
                    public static class SafetySetting
                    {
                        /// <summary>
                        /// Block harm contents or contents about sexual harrassment
                        /// </summary>
                        public const string HARM_CATEGORY_HARASSMENT = "HARM_CATEGORY_HARASSMENT";

                        /// <summary>
                        /// Block nothing, allow all kinds of contents
                        /// </summary>
                        public const string BLOCK_NONE = "BLOCK_NONE";
                    }
                }
            }
        }


        public static class Executions
        {
            public static class Descriptions
            {
                /// <summary>
                /// <see cref="global::Models.StatusJsonModel.Description"/> of <see cref="global::Models.StatusJsonModel"/> used in <seealso cref="global::AiUtility.GeminiUtilityServices.GeminiSessionManager.ExecuteWithToolSupportAsync"/> method
                /// </summary>
                public const string EXECUTE_WITH_TOOL_SUPPORT_ASYNC_DESCRIPTION = $"{ExecutionStatus.EXECUTE} the {AiApi.GeminiAiStudio.AiSchema.Roles.USER} {ToolTasks.TASK} with tool support, and automatically {Vocabulary.MANAGE} the {AiModels.TOKEN}.";
            }
        }

        public static class ExecutionStatus
        {
            public const string CANCELLED = "cancelled";
            public const string EXECUTE = "execute";
            public const string EXECUTED = "executed";
            public const string EXECUTING = "executing";
            public const string COMPLETED = "completed";
            public const string COMPLETES = "completes";
            public const string TASK_COMPLETED = $"{ToolTasks.TASK} is {COMPLETED}";
            public const string AI_COMPLETES_TASK = $"{AiModels.AI} {COMPLETES} the {ToolTasks.TASK}";
            public const string FAILED = "failed";
            public const string FAILURE = "failure";
            public const string ERROR = "error";

        }

        public static class Constraints
        {
            public static class ValueConstraints
            {
                public const string MUST_BE_NONNEGATIVE = $"must be {Vocabulary.NONNEGATIVE}";
                public const string VALUE_MUST_BE_NONNEGATIVE = $"{Vocabulary.VALUE} {MUST_BE_NONNEGATIVE}";
                public const string TIMEOUT_MUST_BE_NONNEGATIVE = $"{Vocabulary.TIMEOUT} must be {Vocabulary.NONNEGATIVE}";

                public const string MUST_BE_POSITIVE = $"must be {Vocabulary.POSITIVE}";
                public const string VALUE_MUST_BE_POSITIVE = $"{Vocabulary.VALUE} {MUST_BE_POSITIVE}";
                public const string MAX_OUTPUT_TOKENS_MUST_BE_POSITIVE = $"{AiModels.MAX_OUTPUT_TOKEN} {Vocabulary.VALUE} {MUST_BE_POSITIVE}";

                public const string MUST_BETWEEN_ZERO_AND_TWO = $"must between 0 to 2";
                public const string TEMPERATURE_MUST_BETWEEN_ZERO_AND_TWO = $"{AiModels.TEMPERATURE} {MUST_BETWEEN_ZERO_AND_TWO}";

                public static readonly string MAX_OUTPUT_TOKENS_MUST_BETWEEN_ZERO_AND_AVAILABLE_MAX_OUTPUT_TOKENS =$"{MAX_OUTPUT_TOKENS_MUST_BE_POSITIVE} and less than {ExecutionSettings.AVAILABLE_MAX_TOKENS}";

                public const string MUST_BE_NONEMPTY = $"can not be null or empty";
                public const string PROMPT_MUST_BE_NONEMPTY = $"{AiModels.PROMPT} {MUST_BE_NONEMPTY}";
                public const string CONTENT_MUST_BE_NONEMPTY = $"{AiModels.CONTENT} {MUST_BE_NONEMPTY}";
            }

            public static class UnsupportedFormat
            {
                public const string UNSUPPORTED_IMAGE_FORMAT = $"{Vocabulary.UNSUPPORTED} {Vocabulary.IMAGE} {Vocabulary.FORMAT}";
            }
        }

        public static class Messages
        {
            public static class FailureMessages
            {
                public const string RUNTIME_EXCEPTION_OCCURRED = "Runtime exception occurred";
                /// <summary>
                /// An error occured due to reach max limits (<seealso cref="global::"/>
                /// </summary>
                public const string MAX_STEPS_REACHED_FORMAT = $"Maximum step limit reached ({{0}} steps). This may be due to an incorrect tool response format preventing the {AiModels.AI} from parsing the conversation correctly. Please verify if the tool output matches the expected format and ensure successful execution.";

                /// <summary>
                /// Overall error message when calling AI API fails. 
                /// </summary>
                public const string AI_API_RUNTIME_EXCEPTION = $"{RUNTIME_EXCEPTION_OCCURRED} while calling the {AiModels.AI} {Vocabulary.API}.";

                /// <summary>
                /// Detailed error message when calling AI API fails.
                /// </summary>
                public const string AI_API_RUNTIME_EXCEPTION_WITH_DETAILS = $"{RUNTIME_EXCEPTION_OCCURRED} while calling the {AiModels.AI} {Vocabulary.API}. Please check 'OverallErrorMessage' and 'DetailedErrorMessage' for more details.";

                public const string AI_API_RUNTIME_PARSE_EXCEPTION = $"{Vocabulary.PARSE} {Vocabulary.EXCEPTION}!!! {RUNTIME_EXCEPTION_OCCURRED} while calling the {AiModels.AI} {Vocabulary.API}. Can not parse the response from {AiModels.AI_MODEL}";

                public const string AI_RETURNS_NULL_RESPONSE = $"{RUNTIME_EXCEPTION_OCCURRED} while calling the {AiModels.AI} {Vocabulary.API}. The {AiModels.AI} {Vocabulary.API} returns null response";
            }
        }
    }
}
