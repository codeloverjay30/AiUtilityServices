using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.ToolKits.Abstractions;
using AiUtility.ToolKits.Executor;
using CustomDataAnnotations.Maintenance;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ThreadLevelLockingUtilityServices;

using static AiUtility.AiBaseUtilityServices.Consts.Constants;
using ExceptionFactories;
using CommonModels;

namespace AiUtility.GeminiUtilityServices.Services
{
    public partial class GeminiSessionManager(
        ILoggerFactoryBaseUtilityService loggerFactoryService ,
        IGeminiConversationManager conversationManager ,
        IGeminiToolService toolService ,
        IGeminiToolExecutor toolExecutor ,
        ISemaphoreSlimService semaphoreService
    ) : IGeminiSessionManager
    {
        [LoggerMessage(Level = LogLevel.Error , Message = "An unknown error while executing `ExecuteWithToolSupportAsync` method!!! The exception is {ex}")]
        static partial void LogExceptionWhenExecutingTool(ILogger logger , Exception ex);
        [LoggerMessage(Level = LogLevel.Error , Message = "The failure ({FailureMessage}) occurred while executing `ExecuteWithToolSupportAsync` method!!!")]
        static partial void LogFailureWhenExecutingTool(ILogger logger , string FailureMessage);

        [LoggerMessage(Level = LogLevel.Information , Message = "Starting AI Workflow for task: {TaskName}, Current Memory Tokens: {Tokens}")]
        static partial void LogBeforeStartToExecuteTool(ILogger logger , string TaskName , int Tokens);

        [LoggerMessage(Level = LogLevel.Information , Message = "Finish AI Workflow for task: {TaskName}, Current Memory Tokens: {Tokens}")]
        static partial void LogAfterFinishExecutingTool(ILogger logger , string TaskName , int Tokens);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executing Gemini tool. ToolName={ToolName}")]
        private static partial void LogGeminiToolExecuting(
            ILogger logger,
            string ToolName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Gemini tool completed. ToolName={ToolName}")]
        private static partial void LogGeminiToolCompleted(
            ILogger logger,
            string ToolName);

        /// <summary>
        /// Logs the structure of a Gemini candidate response.
        /// </summary>
        /// <param name="logger">
        /// The logger instance.
        /// </param>
        /// <param name="partCount">
        /// The number of parts returned by Gemini.
        /// </param>
        /// <param name="functionCallCount">
        /// The number of function calls contained in the response.
        /// </param>
        /// <param name="hasText">
        /// Indicates whether textual content exists in the response.
        /// </param>
        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Gemini response received. PartCount={PartCount}, " +
                "FunctionCallCount={FunctionCallCount}, HasText={HasText}")]
        private static partial void LogGeminiResponseStructure(
            ILogger logger,
            int partCount,
            int functionCallCount,
            bool hasText);

        /// <summary>
        /// Logs the final textual response returned by Gemini.
        /// </summary>
        /// <param name="logger">
        /// The logger instance.
        /// </param>
        /// <param name="message">
        /// The final textual response.
        /// </param>
        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Gemini final response: {Message}")]
        private static partial void LogGeminiFinalResponse(
            ILogger logger,
            string message);

        /// <summary>
        /// Logs an exception that occurs while executing a Gemini tool.
        /// </summary>
        /// <param name="logger">
        /// The logger instance.
        /// </param>
        /// <param name="exception">
        /// The exception raised while executing the tool.
        /// </param>
        /// <param name="toolName">
        /// The name of the Gemini tool that failed.
        /// </param>
        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Gemini tool execution failed. ToolName={ToolName}")]
        private static partial void LogGeminiToolExecutionFailed(
            ILogger logger,
            Exception exception,
            string toolName);


        private readonly ILoggerFactoryBaseUtilityService _loggerFactoryService = loggerFactoryService;
        public ILoggerFactoryBaseUtilityService LoggerFactoryService => _loggerFactoryService;

        private ILogger _logger => _loggerFactoryService.Logger;

        /// <summary>
        /// Conversation manager
        /// </summary>
        private readonly IGeminiConversationManager _conversationManager = conversationManager;
        public IGeminiConversationManager ConversationManager => _conversationManager;

        /// <summary>
        /// Convert the cached metadata from <see cref="GeminiToolMetadata"/> to Json data that Gemini AI Studio accepts.
        /// </summary>

        private readonly IGeminiToolService _toolService = toolService;
        public IGeminiToolService ToolService => _toolService;

        /// <summary>
        /// Auto executes the method that are stored in cached <see cref="GeminiToolMetadata"/>
        /// </summary>
        private readonly IGeminiToolExecutor _toolExecutor = toolExecutor;
        public IGeminiToolExecutor ToolExecutor => _toolExecutor;

        /// <summary>
        /// <see cref="ISemaphoreSlimService"/>
        /// </summary>
        private readonly ISemaphoreSlimService _semaphoreService = semaphoreService;
        public ISemaphoreSlimService SemaphoreService => _semaphoreService;

        /// <inheritdoc cref="ExecuteWithToolSupportAsync{TProgress}(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken, IProgress{TProgress}?)"/>
        /// For the extremely better performance, consider <seealso cref="ExecuteWithToolSupportAsync{TProgress}(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken, IProgress{TProgress}?)" /> method which has features: zero allocations.
        [Obsolete("For the extremely better performance, consider <seealso cref=\"ExecuteWithToolSupportAsync{TProgress}(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken, IProgress{TProgress}?)\" /> method which has features: zero allocations.")]
        [TechnicalDebt(CategoryType.ExecutedTimePerformanceIssue, "ExecuteWithToolSupportAsync{TProgress}(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken, IProgress{TProgress}?)")]
        public async Task<StatusJsonModels> ExecuteWithToolSupportAsync<TProgress>(
            GeminiGenerateRequest request ,
            string userTask ,
            AiExecutionSettings settings ,
            CancellationToken ct = default ,
            IProgress<TProgress>? progressBar = null
        ) where TProgress : WorkflowProgress, new() // 限制必須繼承自基礎模型
        {
            ArgumentNullException.ThrowIfNull(
                userTask);

            return await ExecuteWithToolSupportAsync<TProgress>(
                request,
                userTask.AsMemory(),
                settings,
                ct,
                progressBar);
        }
        /// <summary>
        /// Register the method of custom <seealso cref="Attribute"/> (or its subclass)
        /// then execute the <paramref name="userTask"/>
        /// </summary>
        /// <param name="request">request</param>
        /// <param name="userTask">user task</param>
        /// <param name="settings"><seealso cref="AiExecutionSettings"/></param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="progressBar">progress that will be shown on progress bar.It is neither displayed nor updated when it is specified to null.</param>
        /// <returns></returns>
        public async Task<StatusJsonModels> ExecuteWithToolSupportAsync<TProgress>(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userTask ,
            AiExecutionSettings settings ,
            CancellationToken ct = default ,
            IProgress<TProgress>? progressBar = null
        ) where TProgress : WorkflowProgress, new() // 限制必須繼承自基礎模型
        {

            ArgumentNullException.ThrowIfNull(
                request);

            ArgumentNullException.ThrowIfNull(
                settings);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                settings.MaxSteps,
                nameof(settings.MaxSteps));

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                settings.ToolExecutionTimeout.TotalMilliseconds,
                nameof(settings.ToolExecutionTimeout));

            if (userTask.IsEmpty)
            {
                throw new ArgumentException(
                    "User task cannot be empty.",
                    nameof(userTask));
            }

            WorkflowCompletionStatus workflowStatus =
                WorkflowCompletionStatus.InProgress;

            TaskExecutionStatus taskStatus =
                TaskExecutionStatus.Unknown;

            StatusJsonModels statusJsonModels =
                new StatusJsonModels();
            StatusJsonModel statusJsonModel = new StatusJsonModel
            {
                CategoryName = "ExecuteWithToolSupportAsync" ,
                Description = Constants.Executions.Descriptions.EXECUTE_WITH_TOOL_SUPPORT_ASYNC_DESCRIPTION ,
                Metadata =
                    CreateExecutionMetadata(
                        settings.Metadata,
                        workflowStatus,
                        taskStatus),
            };

            var message = ReadOnlyMemory<char>.Empty;
            int currentStep = 0;

            var maxSteps =
                Math.Min(
                    settings.MaxSteps,
                    Constants.ExecutionSettings.MAX_STEPS);

            var p = new TProgress
            {
                Percentage = 0 + Constants.ProgressBars.BASE_OFFSET_PERCENTAGE ,
                CurrentStep = currentStep ,
                MaxSteps = maxSteps ,
                CurrentAction = Constants.ToolTasks.PREPARE_TO_EXECUTE_TASK ,
                Metadata =
                    CreateExecutionMetadata(
                        settings.Metadata,
                        workflowStatus,
                        taskStatus),
            };
            try
            {
                progressBar?.Report(p);
                LogBeforeStartToExecuteTool(_logger, "ExecuteWithToolSupportAsync", _conversationManager.LastTotalTokens);
                var valueTask = await _semaphoreService.LockWithTimeoutValueAsync(
                    ct,
                    Constants.Timeouts.DEFAULT_TIMEOUTS,
                    false
                );

                // 1. 同步工具定義 (將 Registry 裡的工具轉換為 Gemini API 格式)
                _toolService.SyncToolsToRequest(request);

                // 2. 加入使用者提示詞
                request.AddUserMessage(userTask);

                // 進入執行迴圈（處理潛在的多步 Function Calling）
                while (currentStep < maxSteps)
                {
                    ct.ThrowIfCancellationRequested(); // 確保能立即反應取消請求
                    currentStep++;

                    // 回報一個稍微增加的數值，代表「開始傳送請求」

                    p = new TProgress
                    {
                        Percentage = (int)((double)(currentStep - 1) / maxSteps * Constants.ProgressBars.COMPLETED_PERCENTAGE) + Constants.ProgressBars.BASE_OFFSET_PERCENTAGE,
                        CurrentStep = currentStep,
                        MaxSteps = maxSteps,
                        CurrentAction = Constants.ToolTasks.PREPARE_TO_SEND_PROMPT_TO_AI_MODEL,
                        Metadata =
                            CreateExecutionMetadata(
                                settings.Metadata,
                                workflowStatus,
                                taskStatus),
                    };
                    progressBar?.Report(p);

                    var response = await _conversationManager.SendMessageAsync(
                        request,
                        userTask,
                        settings,
                        ct
                    );

                    p = new TProgress
                    {
                        Percentage = (int)((double)currentStep / maxSteps * ProgressBars.COMPLETED_PERCENTAGE),
                        CurrentStep = currentStep,
                        MaxSteps = maxSteps,
                        CurrentAction = string.Format(Constants.ToolTasks.AI_EXECUTING_TASK, "ExecuteWithToolSupportAsync"),
                        Metadata =
                            CreateExecutionMetadata(
                                settings.Metadata,
                                workflowStatus,
                                taskStatus),
                    };

                    progressBar?.Report(p);

                    var candidate =
                        response?.Candidates?.FirstOrDefault();

                    var parts =
                        candidate?.Content?.Parts;

                    if (candidate is null ||
                        parts is null ||
                        parts.Count == 0)
                    {
                        workflowStatus =
                            WorkflowCompletionStatus.Failed;

                        taskStatus =
                            TaskExecutionStatus.Unknown;

                        const string errorMessage =
                            Constants.Messages.FailureMessages
                                .AI_RETURNS_NULL_RESPONSE;

                        statusJsonModel.IsSuccess = false;
                        statusJsonModel.Result =
                            Constants.ExecutionStatus.ERROR;
                        statusJsonModel.OverallErrorMessage =
                            errorMessage;
                        statusJsonModel.ErrorMessage =
                            errorMessage;
                        statusJsonModel.DetailedErrorMessage =
                            errorMessage;

                        statusJsonModel.Metadata =
                            CreateExecutionMetadata(
                                settings.Metadata,
                                workflowStatus,
                                taskStatus);

                        statusJsonModels.StatusList.Add(
                            statusJsonModel);

                        p = new TProgress
                        {
                            Percentage =
                                (int)(
                                    (double)Math.Max(currentStep - 1, 0)
                                    / maxSteps
                                    * ProgressBars.COMPLETED_PERCENTAGE),

                            CurrentStep =
                                currentStep,

                            MaxSteps =
                                maxSteps,

                            CurrentAction =
                                Constants.ExecutionStatus.ERROR,

                            Metadata =
                                CreateExecutionMetadata(
                                    settings.Metadata,
                                    workflowStatus,
                                    taskStatus),
                        };

                        progressBar?.Report(p);

                        return statusJsonModels;
                    }

                    var functionCalls =
                        parts
                            .Where(static part =>
                                part.FunctionCall is not null)
                            .Select(static part =>
                                part.FunctionCall!)
                            .ToList();

                    var hasText =
                        parts.Any(
                            static part =>
                                !part.RawText.IsEmpty);

                    LogGeminiResponseStructure(
                        _logger,
                        parts.Count,
                        functionCalls.Count,
                        hasText);

                    // 檢查是否為 FunctionCall (AI 要求執行工具)
                    if (functionCalls.Count > 0)
                    {
                        // AI 要求執行工具
                        workflowStatus =
                            WorkflowCompletionStatus.InProgress;

                        taskStatus =
                            TaskExecutionStatus.Unknown;

                        request.AddMessage(candidate!.Content);
                        var responseParts = new List<GeminiPart>();

                        // 判斷是否要分別執行FunctionCall
                        if (settings.ForceSequentialToolExecution)
                        {
                            // 需要分別執行FunctionCall
                            foreach (var call in functionCalls)
                            {
                                ct.ThrowIfCancellationRequested();

                                var taskResult =
                                    await ExecuteAsync(
                                        call,
                                        settings,
                                        ct);

                                statusJsonModels.StatusList.Add(
                                    taskResult.Status);

                                responseParts.Add(
                                    taskResult.Part);
                            }
                        }
                        else
                        {
                            // 並行執行所有的FunctionCall

                            // 建立所有執行任務 (並行啟動)
                            var tasks =
                                functionCalls.Select(
                                    call =>
                                        ExecuteAsync(
                                            call,
                                            settings,
                                            ct));
                            // 等待所有工具執行完畢
                            var taskResults = await Task.WhenAll(tasks);
                            statusJsonModels.StatusList.AddRange(taskResults.Select(r => r.Status));
                            responseParts = taskResults.Select(r => r.Part).ToList();
                        }
                        request.AddMessage(new GeminiMessage
                        {
                            Role = AiApi.GeminiAiStudio.AiSchema.FunctionCall.USER, // "user"
                            Parts = responseParts
                        });
                        continue;

                    }

                    var textPart =
                        parts.FirstOrDefault(
                            static part =>
                                !part.RawText.IsEmpty);

                    if (textPart is not null)
                    {
                        workflowStatus =
                            WorkflowCompletionStatus.Completed;

                        taskStatus =
                            TaskExecutionStatus.Unknown;

                        request.AddMessage(
                            candidate.Content);

                        message =
                            textPart.RawText;

                        var finalMessage =
                            message.ToString();

                        var logMessage =
                            finalMessage.Length <= 500
                                ? finalMessage
                                : string.Concat(
                                    finalMessage.AsSpan(
                                        0,
                                        500),
                                    "...");

                        LogGeminiFinalResponse(
                            _logger,
                            logMessage);

                        statusJsonModel.IsSuccess = true;

                        statusJsonModel.Result =
                            finalMessage;

                        statusJsonModel.OverallErrorMessage =
                            string.Empty;

                        statusJsonModel.ErrorMessage =
                            string.Empty;

                        statusJsonModel.DetailedErrorMessage =
                            string.Empty;

                        statusJsonModel.Metadata =
                            CreateExecutionMetadata(
                                settings.Metadata,
                                workflowStatus,
                                taskStatus);

                        statusJsonModels.StatusList.Add(
                            statusJsonModel);

                        p = new TProgress
                        {
                            Percentage =
                                ProgressBars.COMPLETED_PERCENTAGE,

                            CurrentStep =
                                currentStep,

                            MaxSteps = maxSteps,
                            CurrentAction =
                                Constants.ExecutionStatus.AI_COMPLETES_TASK,

                            Metadata =
                                CreateExecutionMetadata(
                                    settings.Metadata,
                                    workflowStatus,
                                    taskStatus),

                        };

                        progressBar?.Report(
                            p);

                        return statusJsonModels;
                    }

                    workflowStatus =
                        WorkflowCompletionStatus.Failed;

                    taskStatus =
                        TaskExecutionStatus.Unknown;

                    const string unsupportedResponseMessage =
                        "Gemini returned neither a function call nor textual content.";

                    LogFailureWhenExecutingTool(
                        _logger,
                        unsupportedResponseMessage);

                    statusJsonModel.IsSuccess = false;
                    statusJsonModel.Result =
                        Constants.ExecutionStatus.ERROR;
                    statusJsonModel.OverallErrorMessage =
                        unsupportedResponseMessage;
                    statusJsonModel.ErrorMessage =
                        unsupportedResponseMessage;
                    statusJsonModel.DetailedErrorMessage =
                        unsupportedResponseMessage;
                    statusJsonModel.Metadata =
                        CreateExecutionMetadata(
                            settings.Metadata,
                            workflowStatus,
                            taskStatus);

                    statusJsonModels.StatusList.Add(
                        statusJsonModel);

                    p = new TProgress
                    {
                        Percentage =
                            (int)(
                                (double)currentStep
                                / maxSteps
                                * ProgressBars.COMPLETED_PERCENTAGE),

                        CurrentStep =
                            currentStep,

                        MaxSteps =
                            maxSteps,

                        CurrentAction =
                            Constants.ExecutionStatus.ERROR,

                        Metadata =
                            CreateExecutionMetadata(
                                settings.Metadata,
                                workflowStatus,
                                taskStatus),
                    };

                    progressBar?.Report(
                        p);

                    return statusJsonModels;
                }
            }
            catch (OperationCanceledException)
            {
                workflowStatus =
                    WorkflowCompletionStatus.Cancelled;

                taskStatus =
                    TaskExecutionStatus.Unknown;

                // Preserve cancellation semantics for the caller.
                throw;
            }
            catch (Exception exception)
            {
                workflowStatus =
                    WorkflowCompletionStatus.Failed;

                taskStatus =
                    TaskExecutionStatus.Unknown;

                var exceptionUtilityService = new ExceptionHandlingUtilityServices.ExceptionUtilityService(exception);
                exceptionUtilityService.FlattenAndProcess((ex) =>
                {
                    LogExceptionWhenExecutingTool(_logger, ex);
                    statusJsonModels.StatusList.Add(new StatusJsonModel()
                    {
                        IsSuccess = false,
                        Result = AiUtility.AiBaseUtilityServices.Consts.Constants.Messages.FailureMessages.AI_API_RUNTIME_EXCEPTION_WITH_DETAILS,
                        OverallErrorMessage = Constants.Messages.FailureMessages.AI_API_RUNTIME_EXCEPTION,
                        ErrorMessage = ex.Message,
                        DetailedErrorMessage = new ExceptionFactory(ex).Create(),
                        Metadata =
                            CreateExecutionMetadata(
                                settings.Metadata,
                                workflowStatus,
                                taskStatus)
                    });
                });

                p = new TProgress
                {
                    Percentage = (int)((double)(currentStep - 1) / maxSteps * AiUtility.AiBaseUtilityServices.Consts.Constants.ProgressBars.COMPLETED_PERCENTAGE),
                    CurrentStep = currentStep,
                    MaxSteps = maxSteps,
                    CurrentAction = Constants.ExecutionStatus.ERROR,
                    Metadata =
                        CreateExecutionMetadata(
                            settings.Metadata,
                            workflowStatus,
                            taskStatus),
                };
                progressBar?.Report(p);

                return statusJsonModels;
            }
            finally
            {
                LogAfterFinishExecutingTool(
                    _logger,
                    nameof(ExecuteWithToolSupportAsync),
                    _conversationManager.LastTotalTokens);
            }


            workflowStatus =
                WorkflowCompletionStatus.Failed;

            taskStatus =
                TaskExecutionStatus.Unknown;

            message =
                string.Format(
                    AiUtility.AiBaseUtilityServices.Consts.Constants
                        .Messages.FailureMessages.MAX_STEPS_REACHED_FORMAT,
                    maxSteps)
                .AsMemory();

            var messageStr =
                message.ToString();

            LogFailureWhenExecutingTool(
                _logger,
                messageStr);

            statusJsonModel.IsSuccess = false;
            statusJsonModel.Result =
                messageStr;
            statusJsonModel.OverallErrorMessage =
                messageStr;
            statusJsonModel.ErrorMessage =
                messageStr;
            statusJsonModel.DetailedErrorMessage =
                messageStr;
            statusJsonModel.Metadata =
                CreateExecutionMetadata(
                    settings.Metadata,
                    workflowStatus,
                    taskStatus);

            statusJsonModels.StatusList.Add(
                statusJsonModel);

            p = new TProgress
            {
                Percentage =
                    ProgressBars.COMPLETED_PERCENTAGE,

                CurrentStep =
                    currentStep,

                MaxSteps =
                    maxSteps,

                CurrentAction =
                    Constants.ExecutionStatus.ERROR,

                Metadata =
                    CreateExecutionMetadata(
                        settings.Metadata,
                        workflowStatus,
                        taskStatus),
            };

            progressBar?.Report(
                p);

            return statusJsonModels;
        }

        /// <inheritdoc cref="WithExecuteWithToolSupportAsync{TProgress}(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken, IProgress{TProgress}?)"/>
        /// For the extremely better performance, consider <seealso cref="WithExecuteWithToolSupportAsync{TProgress}(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken, IProgress{TProgress}?)"/> method which has features: zero allocations.
        /// </remarks>
        [Obsolete("For the extremely better performance, consider <seealso cref=\"WithExecuteWithToolSupportAsync{TProgress}(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken, IProgress{TProgress}?)\"/> method which has features: zero allocations.")]
        [TechnicalDebt(CategoryType.ExecutedTimePerformanceIssue , "WithExecuteWithToolSupportAsync{TProgress}(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken, IProgress{TProgress}?)")]
        public async Task<StatusJsonModels> WithExecuteWithToolSupportAsync<TProgress>(
            GeminiGenerateRequest request ,
            string userTask ,
            AiExecutionSettings settings ,
            CancellationToken ct = default ,
            IProgress<TProgress>? progressBar = null
        ) where TProgress : WorkflowProgress, new() // 限制必須繼承自基礎模型
        {
            ArgumentNullException.ThrowIfNull(userTask);

            return await WithExecuteWithToolSupportAsync(request, userTask.AsMemory(), settings, ct, progressBar);
        }

        /// <summary>
        /// Executes the Gemini workflow with tool support.
        /// </summary>
        /// <typeparam name="TProgress">
        /// The workflow progress model type.
        /// </typeparam>
        /// <param name="request">
        /// The Gemini generation request.
        /// </param>
        /// <param name="userTask">
        /// The user task to execute.
        /// </param>
        /// <param name="settings">
        /// The AI execution settings.
        /// </param>
        /// <param name="ct">
        /// The cancellation token.
        /// </param>
        /// <param name="progressBar">
        /// The optional progress reporter.
        /// </param>
        /// <returns>
        /// The workflow execution result.
        /// </returns>
        public Task<StatusJsonModels> WithExecuteWithToolSupportAsync<TProgress>(
            GeminiGenerateRequest request,
            ReadOnlyMemory<char> userTask,
            AiExecutionSettings settings,
            CancellationToken ct = default,
            IProgress<TProgress>? progressBar = null)
            where TProgress : WorkflowProgress, new()
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ArgumentNullException.ThrowIfNull(
                settings);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                settings.MaxSteps,
                nameof(settings.MaxSteps));


            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                settings.ToolExecutionTimeout.TotalMilliseconds,
                nameof(settings.ToolExecutionTimeout));

            if (userTask.IsEmpty)
            {
                throw new ArgumentException(
                    "User task cannot be empty.",
                    nameof(userTask));
            }

            return ExecuteWithToolSupportAsync(
                request,
                userTask,
                settings,
                ct,
                progressBar);
        }

        /// <summary>
        /// Helper method to execute the function call (<paramref name="call"/>) with settings (<paramref name="settings"/>).
        /// </summary>
        /// <param name="call"><see cref="GeminiFunctionCall"/></param>
        /// <param name="settings"><see cref="AiExecutionSettings"/></param>
        /// <param name="globalCt">Cancellation token</param>
        /// <returns>
        /// A record containg one part (<see cref="GeminiPart"/> type) and execution status (<see cref="StatusJsonModel"/>)
        /// </returns>
        private async Task<(GeminiPart Part , StatusJsonModel Status)> ExecuteAsync(
            GeminiFunctionCall call ,
            AiExecutionSettings settings ,
            CancellationToken globalCt = default
        )
        {
            // 建立一個僅針對此工具執行的超時 Token
            using var toolTimeoutCts = new CancellationTokenSource(settings.ToolExecutionTimeout);
            // 將全域取消與工具超時連結起來
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(globalCt , toolTimeoutCts.Token);

            var linkedCt = linkedCts.Token;

            try
            {
                LogGeminiToolExecuting(
                    _logger,
                    call.Name);

                var result = await _toolExecutor.ExecuteAsync(
                    call.Name,
                    call.Args.ToDictionary(k => k.Key, v => (object)v.Value),
                    linkedCt
                );

                LogGeminiToolCompleted(
                    _logger,
                    call.Name);

                return (
                    Part: new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse { Name = call.Name , Response = CreateFunctionResponse(result) }
                    } ,
                    Status: new StatusJsonModel
                    {
                        IsSuccess = true ,
                        Result = JsonSerializer.Serialize(result) ,
                        DataSource = $"{call.Name} (Args: {JsonSerializer.Serialize(call.Args)})" ,
                        OverallErrorMessage = string.Empty ,
                        ErrorMessage = string.Empty ,
                        DetailedErrorMessage = string.Empty ,
                        Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                    }
                 );
            }
            catch(OperationCanceledException ex) when(toolTimeoutCts.IsCancellationRequested)
            {
                var errorMessage = AiUtility.AiBaseUtilityServices.Consts.Constants.ToolTasks.TASK_IS_CANCELLED_OR_ENCOUNTERS_TIMEOUT;
                LogFailureWhenExecutingTool(_logger , $"{errorMessage} with error message: {ex.Message}");
                return (
                    Part: new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            Name = call.Name ,
                            Response = JsonSerializer.SerializeToElement(
                                new
                                {
                                    success = false,
                                    status = "error",
                                    message = ex.Message
                                })
                        }
                    } ,
                    Status: new StatusJsonModel
                    {
                        IsSuccess = false ,
                        DataSource = $"{call.Name} (Args: {JsonSerializer.Serialize(call.Args)})" ,
                        Result = "Error" ,
                        OverallErrorMessage = errorMessage ,
                        ErrorMessage = $"{errorMessage} with error message {ex.Message}" ,
                        DetailedErrorMessage = new ExceptionFactory(ex).Create() ,
                        Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                    }
                );
            }
            catch(Exception ex) when(ex is not OperationCanceledException)
            {
                var errorMessage = "An unknown error occured!!!";

                LogGeminiToolExecutionFailed(
                    _logger,
                    ex,
                    call.Name);

                // 將錯誤餵回給 AI，讓它有機會進行補救或重新識別 UI
                return (
                    Part: new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            Name = call.Name ,
                            Response = JsonSerializer.SerializeToElement(
                            new
                            {
                                success = false,
                                status = "error",
                                message = ex.Message
                            })
                        }
                    } ,
                    Status: new StatusJsonModel
                    {
                        IsSuccess = false ,
                        DataSource = $"{call.Name} (Args: {JsonSerializer.Serialize(call.Args)})" ,
                        Result = "Error" ,
                        OverallErrorMessage = errorMessage ,
                        ErrorMessage = ex.Message ,
                        DetailedErrorMessage = new ExceptionFactory(ex).Create() ,
                        Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                    }
                );
            }
        }


        /// <inheritdoc cref="ExecuteAutomationStepAsync(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken)"/>
        /// <remarks>
        /// For the extremely better performance, consider <seealso cref="ExecuteAutomationStepAsync(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken)"/> method which has features: zero allocations.
        /// </remarks>
        [Obsolete("For the extremely better performance, consider <seealso cref=\"ExecuteAutomationStepAsync(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken)\"/> method which has features: zero allocations.")]
        [TechnicalDebt(CategoryType.ExecutedTimePerformanceIssue , "ExecuteAutomationStepAsync(GeminiGenerateRequest, ReadOnlyMemory{char}, AiExecutionSettings, CancellationToken)")]
        public async Task<string> ExecuteAutomationStepAsync(
            GeminiGenerateRequest request ,
            string userTask ,
            AiExecutionSettings settings ,
            CancellationToken ct = default
        )
        {
            var response = await _conversationManager.SendMessageAsync(
                request ,
                userTask ,
                settings ,
                ct
            );

            return response.Text;
        }

        /// <summary>
        /// Execute one task automatically and manage the token
        /// </summary>
        /// <param name="request">user request</param>
        /// <param name="userTask">the task that will be executed</param>
        /// <param name="settings"><seealso cref="AiExecutionSettings"/></param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>response representing as string</returns>
        public async Task<string> ExecuteAutomationStepAsync(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userTask ,
            AiExecutionSettings settings ,
            CancellationToken ct = default
        )
        {
            var response = await _conversationManager.SendMessageAsync(
                request ,
                userTask ,
                settings ,
                ct
            );

            return response.Text;
        }

        /// <summary>
        /// Save the session
        /// </summary>
        /// <param name="request">request</param>
        /// <param name="filePath">destination file path to save the session</param>
        public void SaveSession(
            GeminiGenerateRequest request ,
            string filePath
        )
        {
            var json = JsonSerializer.Serialize(request);
            File.WriteAllText(filePath , json);
        }


        /// <summary>
        /// Load the session
        /// </summary>
        /// <param name="filePath">source file path to load the session</param>
        /// <returns></returns>
        public GeminiGenerateRequest LoadSession(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<GeminiGenerateRequest>(json) ?? throw new InvalidOperationException("無法解析 Session 檔案。");
        }

        /// <summary>
        /// Creates execution metadata containing workflow and business-task status.
        /// </summary>
        /// <param name="source">
        /// The original execution metadata.
        /// </param>
        /// <param name="workflowStatus">
        /// The current Gemini workflow completion status.
        /// </param>
        /// <param name="taskStatus">
        /// The current business-task execution status.
        /// </param>
        /// <returns>
        /// A new metadata dictionary containing the original values and status information.
        /// </returns>
        private static Dictionary<string, string> CreateExecutionMetadata(
            IReadOnlyDictionary<string, string>? source,
            WorkflowCompletionStatus workflowStatus,
            TaskExecutionStatus taskStatus)
        {
            var metadata =
                source is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(
                        source);

            metadata["WorkflowStatus"] =
                workflowStatus.ToString();

            metadata["TaskStatus"] =
                taskStatus.ToString();

            return metadata;
        }

        /// <summary>
        /// Creates a structured Gemini function response from a tool result.
        /// </summary>
        /// <param name="result">
        /// The result returned by the executed tool.
        /// </param>
        /// <returns>
        /// A JSON object suitable for the Gemini function-response wire contract.
        /// </returns>
        internal static JsonElement CreateFunctionResponse(
            object? result)
        {
            if (result is null)
            {
                return JsonSerializer.SerializeToElement(
                    new
                    {
                        success = true,
                    });
            }

            var response =
                JsonSerializer.SerializeToElement(
                    result);

            if (response.ValueKind == JsonValueKind.Object)
            {
                return response;
            }

            return JsonSerializer.SerializeToElement(
                new
                {
                    success = true,
                    result,
                });
        }
    }
}
