using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Models;
using CustomDataAnnotations.Maintenance;
using LoggerFactoryUtilityServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Services
{
    public interface IGeminiConversationManager
    {
        ILoggerFactoryBaseUtilityService LoggerFactoryService { get; }
        IGeminiApiClient Client { get; }

        int LastTotalTokens { get; }
        Task<GeminiResponse> SendMessageAsync(
            GeminiGenerateRequest request ,
            string userText,
            AiExecutionSettings executionSettings,
            CancellationToken ct = default
        );
        Task<GeminiResponse> WithSendMessageAsync(
            GeminiGenerateRequest request ,
            string userText,
            AiExecutionSettings executionSettings,
            CancellationToken ct = default
        );
        Task<GeminiResponse> SendMessageAsync(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userText ,
            AiExecutionSettings executionSettings,
            CancellationToken ct = default
        );
        Task<GeminiResponse> WithSendMessageAsync(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userText ,
            AiExecutionSettings executionSettings,
            CancellationToken ct = default
        );

        Task<GeminiResponse> SendMessageAsync(
            GeminiGenerateRequest request ,
            GeminiMessage message , // 建議改為接收 GeminiMessage 物件，以支援多 Parts (Function Responses)
            AiExecutionSettings settings ,
            CancellationToken ct = default
        );
        Task<GeminiResponse> WithSendMessageAsync(
            GeminiGenerateRequest request ,
            GeminiMessage message , // 建議改為接收 GeminiMessage 物件，以支援多 Parts (Function Responses)
            AiExecutionSettings settings ,
            CancellationToken ct = default
        );


    }
}
