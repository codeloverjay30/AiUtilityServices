using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
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
            return await ExecuteWithToolSupportAsync<TProgress>(request, userTask.AsMemory() , settings , ct, progressBar);
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
            StatusJsonModels statusJsonModels = new StatusJsonModels();
            StatusJsonModel statusJsonModel = new StatusJsonModel
            {
                CategoryName = "ExecuteWithToolSupportAsync" ,
                Description = Constants.Executions.Descriptions.EXECUTE_WITH_TOOL_SUPPORT_ASYNC_DESCRIPTION ,
                Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
            };

            var message = ReadOnlyMemory<char>.Empty;
            var resultText = ReadOnlyMemory<char>.Empty;
            int currentStep = 0;

            var p = new TProgress
            {
                Percentage = 0 + Constants.ProgressBars.BASE_OFFSET_PERCENTAGE ,
                CurrentStep = currentStep ,
                MaxSteps = settings.MaxSteps ,
                CurrentAction = Constants.ToolTasks.PREPARE_TO_EXECUTE_TASK ,
                Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
            };
            try
            {
                progressBar?.Report(p);
                LogBeforeStartToExecuteTool(_logger , "ExecuteWithToolSupportAsync" , _conversationManager.LastTotalTokens);
                var valueTask = await _semaphoreService.LockWithTimeoutValueAsync(
                    ct ,
                    Constants.Timeouts.DEFAULT_TIMEOUTS ,
                    false
                );

                // 1. 同步工具定義 (將 Registry 裡的工具轉換為 Gemini API 格式)
                _toolService.SyncToolsToRequest(request);

                // 2. 加入使用者提示詞
                request.AddUserMessage(userTask);

                // 進入執行迴圈（處理潛在的多步 Function Calling）
                while(currentStep < Constants.ExecutionSettings.MAX_STEPS)
                {
                    ct.ThrowIfCancellationRequested(); // 確保能立即反應取消請求
                    currentStep++;

                    // 回報一個稍微增加的數值，代表「開始傳送請求」

                    p = new TProgress
                    {
                        Percentage = (int)((double)(currentStep - 1) / settings.MaxSteps * Constants.ProgressBars.COMPLETED_PERCENTAGE) + Constants.ProgressBars.BASE_OFFSET_PERCENTAGE ,
                        CurrentStep = currentStep ,
                        MaxSteps = settings.MaxSteps ,
                        CurrentAction = Constants.ToolTasks.PREPARE_TO_SEND_PROMPT_TO_AI_MODEL ,
                        Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                    };
                    progressBar?.Report(p);

                    var response = await _conversationManager.SendMessageAsync(
                        request ,
                        userTask ,
                        settings ,
                        ct
                    );

                    p = new TProgress
                    {
                        Percentage = (int)((double)currentStep / settings.MaxSteps * ProgressBars.COMPLETED_PERCENTAGE) ,
                        CurrentStep = currentStep ,
                        MaxSteps = settings.MaxSteps ,
                        CurrentAction = string.Format(Constants.ToolTasks.AI_EXECUTING_TASK , "ExecuteWithToolSupportAsync") ,
                        Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                    };

                    progressBar?.Report(p);

                    var candidate = response?.Candidates?.FirstOrDefault();
                    var firstPart = candidate?.Content?.Parts?.FirstOrDefault();
                    var functionCalls = candidate?.Content.Parts
                        .Where(p => p.FunctionCall != null)
                        .Select(p => p.FunctionCall)
                        .ToList();

                    if(firstPart == null)
                    {
                        // AI 回覆空的Response
                        p = new TProgress
                        {
                            Percentage = ProgressBars.COMPLETED_PERCENTAGE ,
                            CurrentStep = currentStep ,
                            MaxSteps = settings.MaxSteps ,
                            CurrentAction = Constants.ExecutionStatus.AI_COMPLETES_TASK ,
                            Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                        };

                        statusJsonModel.IsSuccess = false;
                        statusJsonModel.Result = Constants.ExecutionStatus.ERROR; // "error"
                        statusJsonModel.OverallErrorMessage = Constants.Messages.FailureMessages.AI_RETURNS_NULL_RESPONSE;
                        statusJsonModel.ErrorMessage = Constants.Messages.FailureMessages.AI_RETURNS_NULL_RESPONSE;
                        statusJsonModel.DetailedErrorMessage = Constants.Messages.FailureMessages.AI_RETURNS_NULL_RESPONSE;
                        statusJsonModels.StatusList.Add(statusJsonModel);
                        progressBar?.Report(p);
                        return statusJsonModels;
                    }

                    // 4. 檢查是否為文字回應 (AI 給出了最終答案)
                    if(candidate != null && !(firstPart?.RawText.IsEmpty ?? false))
                    {
                        // AI 給了答案

                        // 在回傳前，別忘了把 AI 的最後這句話也加入對話紀錄，保持 Session 連貫
                        p = new TProgress
                        {
                            Percentage = ProgressBars.COMPLETED_PERCENTAGE ,
                            CurrentStep = currentStep ,
                            MaxSteps = settings.MaxSteps ,
                            CurrentAction = Constants.ExecutionStatus.AI_COMPLETES_TASK ,
                            Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                        };
                        request.AddMessage(candidate.Content);
                        message = firstPart?.RawText ?? ReadOnlyMemory<char>.Empty;
                        statusJsonModel.IsSuccess = true;
                        statusJsonModel.Result = message.ToString();
                        statusJsonModels.StatusList.Add(statusJsonModel);
                        progressBar?.Report(p);
                        return statusJsonModels;
                    }

                    // 5. 檢查是否為 FunctionCall (AI 要求執行工具)
                    if(functionCalls != null && functionCalls.Any())
                    {
                        // AI 要求執行工具

                        request.AddMessage(candidate!.Content);
                        var options = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = _semaphoreService.MaxRequestsPerWindow + 1 // 同時最多執行 MaxRequestsPerWindow + 1 個任務
                        };

                        var responseParts = new List<GeminiPart>();

                        // 判斷是否要分別執行FunctionCall
                        if(settings.ForceSequentialToolExecution)
                        {
                            // 需要分別執行FunctionCall
                            foreach(var call in functionCalls)
                            {
                                var taskResult = await ExecuteAsync(call! , settings , ct);
                                statusJsonModels.StatusList.Add(taskResult.Status);
                                responseParts.Add(taskResult.Part);
                            }
                        }
                        else
                        {
                            // 並行執行所有的FunctionCall

                            // 建立所有執行任務 (並行啟動)
                            var tasks = functionCalls.Select(call => ExecuteAsync(call! , settings , ct));
                            // 等待所有工具執行完畢
                            var taskResults = await Task.WhenAll(tasks);
                            statusJsonModels.StatusList.AddRange(taskResults.Select(r => r.Status));
                            responseParts = taskResults.Select(r => r.Part).ToList();
                        }
                        request.AddMessage(new GeminiMessage
                        {
                            Role = AiApi.GeminiAiStudio.AiSchema.FunctionCall.FUNCTION, // "function"
                            Parts = responseParts
                        });
                        continue;

                    }
                    break;
                }
            }
            catch(OperationCanceledException)
            {
                // 讓取消信號正常向外傳遞，不要攔截它
                throw;
            }
            catch(Exception exception)
            {
                var exceptionUtilityService = new ExceptionHandlingUtilityServices.ExceptionUtilityService(exception);
                exceptionUtilityService.FlattenAndProcess((ex) =>
                {
                    LogExceptionWhenExecutingTool(_logger , ex);
                    statusJsonModels.StatusList.Add(new StatusJsonModel()
                    {
                        IsSuccess = false ,
                        Result = AiUtility.AiBaseUtilityServices.Consts.Constants.Messages.FailureMessages.AI_API_RUNTIME_EXCEPTION_WITH_DETAILS ,
                        OverallErrorMessage = Constants.Messages.FailureMessages.AI_API_RUNTIME_EXCEPTION ,
                        ErrorMessage = ex.Message ,
                        DetailedErrorMessage = new ExceptionFactory(ex).Create()
                    });
                });

                p = new TProgress
                {
                    Percentage = (int)((double)(currentStep - 1) / settings.MaxSteps * AiUtility.AiBaseUtilityServices.Consts.Constants.ProgressBars.COMPLETED_PERCENTAGE) ,
                    CurrentStep = currentStep ,
                    MaxSteps = settings.MaxSteps ,
                    CurrentAction = Constants.ExecutionStatus.AI_COMPLETES_TASK ,
                    Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                };
                progressBar?.Report(p);
            }
            finally
            {

            }

            if(currentStep >= settings.MaxSteps)
            {
                message = string.Format(AiUtility.AiBaseUtilityServices.Consts.Constants.Messages.FailureMessages.MAX_STEPS_REACHED_FORMAT , settings.MaxSteps).AsMemory();
                var messageStr = message.ToString();
                LogFailureWhenExecutingTool(_logger , messageStr);
                statusJsonModel.IsSuccess = false;
                statusJsonModel.Result = messageStr;
                statusJsonModel.OverallErrorMessage = messageStr;
                statusJsonModel.ErrorMessage = messageStr;
                statusJsonModel.DetailedErrorMessage = messageStr;
                statusJsonModels.StatusList.Add(statusJsonModel);
                return statusJsonModels;
            }


            p = new TProgress
            {
                Percentage = AiUtility.AiBaseUtilityServices.Consts.Constants.ProgressBars.COMPLETED_PERCENTAGE ,
                CurrentStep = currentStep ,
                MaxSteps = settings.MaxSteps ,
                CurrentAction = AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionStatus.AI_COMPLETES_TASK ,
                Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
            };

            LogAfterFinishExecutingTool(_logger , "ExecuteWithToolSupportAsync" , _conversationManager.LastTotalTokens);
            statusJsonModel.IsSuccess = true;
            statusJsonModel.Result = Constants.ExecutionStatus.AI_COMPLETES_TASK;
            statusJsonModel.OverallErrorMessage = string.Empty;
            statusJsonModel.ErrorMessage = string.Empty;
            statusJsonModel.DetailedErrorMessage = string.Empty;
            statusJsonModels.StatusList.Add(statusJsonModel);
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
            return await WithExecuteWithToolSupportAsync(request , userTask.AsMemory() , settings , ct , progressBar);
        }

        /// <summary>
        /// Enter the prompt as <paramref name="userTask"/> into <paramref name="request"/> and then generate the response
        /// via API call of Gemini AI Studio using <paramref name="settings"/>
        /// </summary>
        /// <typeparam name="TProgress"></typeparam>
        /// <param name="request"></param>
        /// <param name="userTask">user task</param>
        /// <param name="settings"><seealso cref="AiExecutionSettings"/></param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="progressBar">Progress bar that displayed on UI</param>
        /// <returns><see cref="StatusJsonModels"/> represents the execution status or result of many tasks</returns>
        public async Task<StatusJsonModels> WithExecuteWithToolSupportAsync<TProgress>(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userTask ,
            AiExecutionSettings settings ,
            CancellationToken ct = default ,
            IProgress<TProgress>? progressBar = null
        ) where TProgress : WorkflowProgress, new() // 限制必須繼承自基礎模型
        {
            StatusJsonModels statusJsonModels = new StatusJsonModels();
            StatusJsonModel statusJsonModel = new StatusJsonModel
            {
                CategoryName = "ExecuteWithToolSupportAsync" ,
                Description = Constants.Executions.Descriptions.EXECUTE_WITH_TOOL_SUPPORT_ASYNC_DESCRIPTION ,
                Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
            };

            var message = ReadOnlyMemory<char>.Empty;
            var resultText = ReadOnlyMemory<char>.Empty;
            int currentStep = 0;

            var p = new TProgress
            {
                Percentage = 0 + Constants.ProgressBars.BASE_OFFSET_PERCENTAGE ,
                CurrentStep = currentStep ,
                MaxSteps = settings.MaxSteps ,
                CurrentAction = Constants.ToolTasks.PREPARE_TO_EXECUTE_TASK ,
                Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
            };
            try
            {
                progressBar?.Report(p);
                LogBeforeStartToExecuteTool(_logger , "ExecuteWithToolSupportAsync" , _conversationManager.LastTotalTokens);
                var valueTask = await _semaphoreService.LockWithTimeoutValueAsync(
                    ct ,
                    Constants.Timeouts.DEFAULT_TIMEOUTS ,
                    false
                );

                // 1. 同步工具定義 (將 Registry 裡的工具轉換為 Gemini API 格式)
                _toolService.SyncToolsToRequest(request);

                // 2. 加入使用者提示詞
                request = request.WithUserMessage(userTask);

                // 進入執行迴圈（處理潛在的多步 Function Calling）
                while(currentStep < Constants.ExecutionSettings.MAX_STEPS)
                {
                    ct.ThrowIfCancellationRequested(); // 確保能立即反應取消請求
                    currentStep++;

                    // 回報一個稍微增加的數值，代表「開始傳送請求」

                    p = new TProgress
                    {
                        Percentage = (int)((double)(currentStep - 1) / settings.MaxSteps * Constants.ProgressBars.COMPLETED_PERCENTAGE) + Constants.ProgressBars.BASE_OFFSET_PERCENTAGE ,
                        CurrentStep = currentStep ,
                        MaxSteps = settings.MaxSteps ,
                        CurrentAction = Constants.ToolTasks.PREPARE_TO_SEND_PROMPT_TO_AI_MODEL ,
                        Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                    };
                    progressBar?.Report(p);

                    var response = await _conversationManager.WithSendMessageAsync(
                        request ,
                        userTask ,
                        settings ,
                        ct
                    );

                    p = new TProgress
                    {
                        Percentage = (int)((double)currentStep / settings.MaxSteps * ProgressBars.COMPLETED_PERCENTAGE) ,
                        CurrentStep = currentStep ,
                        MaxSteps = settings.MaxSteps ,
                        CurrentAction = string.Format(Constants.ToolTasks.AI_EXECUTING_TASK , "ExecuteWithToolSupportAsync") ,
                        Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                    };

                    progressBar?.Report(p);

                    var candidate = response?.Candidates?.FirstOrDefault();
                    var firstPart = candidate?.Content?.Parts?.FirstOrDefault();
                    var functionCalls = candidate?.Content.Parts
                        .Where(p => p.FunctionCall != null)
                        .Select(p => p.FunctionCall)
                        .ToList();

                    if(firstPart == null)
                    {
                        // 取得prompt(剛剛使用者將prompt加入request)
                        p = new TProgress
                        {
                            Percentage = ProgressBars.COMPLETED_PERCENTAGE ,
                            CurrentStep = currentStep ,
                            MaxSteps = settings.MaxSteps ,
                            CurrentAction = Constants.ExecutionStatus.AI_COMPLETES_TASK ,
                            Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                        };

                        resultText = request.Contents?.FirstOrDefault()?.Parts?.FirstOrDefault()?.RawText ?? ReadOnlyMemory<char>.Empty;
                        statusJsonModel.IsSuccess = true;
                        statusJsonModel.Result = resultText.ToString();
                        statusJsonModels.StatusList.Add(statusJsonModel);
                        progressBar?.Report(p);
                        return statusJsonModels;
                    }

                    // 4. 檢查是否為文字回應 (AI 給出了最終答案)
                    if(candidate != null && !(firstPart?.RawText.IsEmpty ?? false))
                    {
                        // 在回傳前，別忘了把 AI 的最後這句話也加入對話紀錄，保持 Session 連貫
                        p = new TProgress
                        {
                            Percentage = ProgressBars.COMPLETED_PERCENTAGE ,
                            CurrentStep = currentStep ,
                            MaxSteps = settings.MaxSteps ,
                            CurrentAction = Constants.ExecutionStatus.AI_COMPLETES_TASK ,
                            Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                        };
                        request.Contents.Add(candidate.Content);
                        statusJsonModel.IsSuccess = true;
                        statusJsonModel.Result = firstPart?.RawText.ToString() ?? string.Empty;
                        statusJsonModels.StatusList.Add(statusJsonModel);
                        progressBar?.Report(p);
                        return statusJsonModels;
                    }

                    // 5. 檢查是否為 FunctionCall (AI 要求執行工具)
                    if(functionCalls != null && functionCalls.Any())
                    {
                        request = request.WithMessage(candidate.Content);

                        var options = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = _semaphoreService.MaxRequestsPerWindow + 1 // 同時最多執行 MaxRequestsPerWindow + 1 個任務
                        };


                        var responseParts = new List<GeminiPart>();


                        if(settings.ForceSequentialToolExecution)
                        {
                            foreach(var call in functionCalls)
                            {
                                var taskResult = await ExecuteAsync(call! , settings , ct);
                                statusJsonModels.StatusList.Add(taskResult.Status);
                                responseParts.Add(taskResult.Part);
                            }
                        }
                        else
                        {
                            // 建立所有執行任務 (並行啟動)
                            var tasks = functionCalls.Select(call => ExecuteAsync(call! , settings , ct));
                            // 等待所有工具執行完畢
                            var taskResults = await Task.WhenAll(tasks);
                            statusJsonModels.StatusList.AddRange(taskResults.Select(r => r.Status));
                            responseParts = taskResults.Select(r => r.Part).ToList();
                        }
                        request = request.WithMessage(new GeminiMessage
                        {
                            Role = Constants.AiApi.GeminiAiStudio.AiSchema.FunctionCall.FUNCTION , // "function"
                            Parts = responseParts
                        });
                        continue;

                    }
                    break;
                }
            }
            catch(OperationCanceledException)
            {
                // 讓取消信號正常向外傳遞，不要攔截它
                throw;
            }
            catch(Exception exception)
            {
                var exceptionUtilityService = new ExceptionHandlingUtilityServices.ExceptionUtilityService(exception);
                exceptionUtilityService.FlattenAndProcess((ex) =>
                {
                    LogExceptionWhenExecutingTool(_logger , ex);
                    statusJsonModels.StatusList.Add(new StatusJsonModel()
                    {
                        IsSuccess = false ,
                        Result = AiUtility.AiBaseUtilityServices.Consts.Constants.Messages.FailureMessages.AI_API_RUNTIME_EXCEPTION_WITH_DETAILS ,
                        OverallErrorMessage = Constants.Messages.FailureMessages.AI_API_RUNTIME_EXCEPTION ,
                        ErrorMessage = ex.Message ,
                        DetailedErrorMessage = new ExceptionFactory(ex).Create()
                    });
                });

                p = new TProgress
                {
                    Percentage = (int)((double)(currentStep - 1) / settings.MaxSteps * AiUtility.AiBaseUtilityServices.Consts.Constants.ProgressBars.COMPLETED_PERCENTAGE) ,
                    CurrentStep = currentStep ,
                    MaxSteps = settings.MaxSteps ,
                    CurrentAction = Constants.ExecutionStatus.AI_COMPLETES_TASK ,
                    Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
                };
                progressBar?.Report(p);
            }
            finally
            {

            }

            if(currentStep >= settings.MaxSteps)
            {
                var messageStr = string.Format(AiUtility.AiBaseUtilityServices.Consts.Constants.Messages.FailureMessages.MAX_STEPS_REACHED_FORMAT , settings.MaxSteps);
                LogFailureWhenExecutingTool(_logger , messageStr);
                statusJsonModel.IsSuccess = false;
                statusJsonModel.Result = messageStr;
                statusJsonModel.OverallErrorMessage = messageStr;
                statusJsonModel.ErrorMessage = messageStr;
                statusJsonModel.DetailedErrorMessage = messageStr;
                statusJsonModels.StatusList.Add(statusJsonModel);
                return statusJsonModels;
            }


            p = new TProgress
            {
                Percentage = AiUtility.AiBaseUtilityServices.Consts.Constants.ProgressBars.COMPLETED_PERCENTAGE ,
                CurrentStep = currentStep ,
                MaxSteps = settings.MaxSteps ,
                CurrentAction = AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionStatus.AI_COMPLETES_TASK ,
                Metadata = settings.Metadata != null ? new Dictionary<string , string>(settings.Metadata) : new() ,
            };

            LogAfterFinishExecutingTool(_logger , "ExecuteWithToolSupportAsync" , _conversationManager.LastTotalTokens);
            statusJsonModel.IsSuccess = true;
            statusJsonModel.Result = Constants.ExecutionStatus.AI_COMPLETES_TASK;
            statusJsonModel.OverallErrorMessage = string.Empty;
            statusJsonModel.ErrorMessage = string.Empty;
            statusJsonModel.DetailedErrorMessage = string.Empty;
            statusJsonModels.StatusList.Add(statusJsonModel);
            return statusJsonModels;
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
                var result = await _toolExecutor.ExecuteAsync(
                    call.Name ,
                    call.Args.ToDictionary(k => k.Key , v => (object)v.Value) ,
                    linkedCt
                );
                return (
                    Part: new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse { Name = call.Name , Response = JsonSerializer.Serialize(result) }
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
                            Response = JsonSerializer.Serialize(new { status = "error" , message = ex.Message })
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
                LogFailureWhenExecutingTool(_logger , $"工具 {call.Name} 執行失敗，錯誤訊息: {ex.Message}");
                // 將錯誤餵回給 AI，讓它有機會進行補救或重新識別 UI
                return (
                    Part: new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            Name = call.Name ,
                            Response = JsonSerializer.Serialize(new { status = "error" , message = ex.Message })
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
    }
}
