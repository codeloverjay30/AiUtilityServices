using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.ToolKits.Executor;
using CommonModels;
using CustomDataAnnotations.Maintenance;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using System;

using System.Collections.Generic;
using System.Text;
using ThreadLevelLockingUtilityServices;

namespace AiUtility.GeminiUtilityServices.Services
{
    public interface IGeminiSessionManager
    {
        ILoggerFactoryBaseUtilityService LoggerFactoryService { get; }
        IGeminiConversationManager ConversationManager { get; }
        IGeminiToolService ToolService { get; }
        IGeminiToolExecutor ToolExecutor { get; }

        ISemaphoreSlimService SemaphoreService { get; }

        Task<StatusJsonModels> ExecuteWithToolSupportAsync<TProgress>(
            GeminiGenerateRequest request ,
            string userTask ,
            AiExecutionSettings settings,
            CancellationToken ct = default,
            IProgress<TProgress> progress = null
        ) where TProgress:WorkflowProgress,new();
        Task<StatusJsonModels> ExecuteWithToolSupportAsync<TProgress>(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userTask ,
            AiExecutionSettings settings,
            CancellationToken ct = default,
            IProgress<TProgress> progress = null
        ) where TProgress:WorkflowProgress,new();

        Task<string> ExecuteAutomationStepAsync(
            GeminiGenerateRequest request ,
            string userTask,
            AiExecutionSettings settings,
            CancellationToken ct = default
        );
        Task<string> ExecuteAutomationStepAsync(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userTask,
            AiExecutionSettings settings,
            CancellationToken ct = default
        );

        void SaveSession(
            GeminiGenerateRequest request ,
            string filePath
        );

        GeminiGenerateRequest LoadSession(string filePath);
    }
}
