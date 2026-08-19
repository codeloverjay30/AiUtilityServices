using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Models;
using CustomDataAnnotations.Maintenance;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Services
{
    public partial class GeminiConversationManager(
        ILoggerFactoryBaseUtilityService loggerFactoryService,
        IGeminiApiClient client
    ): IGeminiConversationManager
    {
        private readonly ILoggerFactoryBaseUtilityService _loggerFactoryService = loggerFactoryService;
        public ILoggerFactoryBaseUtilityService LoggerFactoryService => _loggerFactoryService;
        private ILogger _logger => _loggerFactoryService.Logger;
        [LoggerMessage(Level = LogLevel.Error,Message = "An exception occured when `SendMessageAsync` is called with error message:{ErrorMessage}")]
        static partial void LogExceptionWhenSendingMessage(ILogger logger , string ErrorMessage);
        private readonly IGeminiApiClient _client = client;
        public IGeminiApiClient Client => _client;
        public int LastTotalTokens { get; private set; }

        /// <summary>
        /// Consolidate the token then send the message to AI Model through API
        /// </summary>
        /// <param name="request">request</param>
        /// <param name="message">message</param>
        /// <param name="settings"><see cref="AiExecutionSettings"/></param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        /// <remarks>
        public async Task<GeminiResponse> SendMessageAsync(
            GeminiGenerateRequest request ,
            GeminiMessage message , // 建議改為接收 GeminiMessage 物件，以支援多 Parts (Function Responses)
            AiExecutionSettings settings,
            CancellationToken ct = default            
        )
        {
            // 1. 自動 Token 檢查與壓縮記憶體 (Memory Consolidation)
            // 在發送新請求前，檢查是否需要壓縮過去的對話以節省空間
            await request.ConsolidateMemoryAsync(
                _client ,
                LastTotalTokens,
                settings
            );

            // 2. 將新訊息加入請求內容
            request.AddMessage(message);

            try
            {
                // 3. 呼叫底層 Gemini API 
                // 這裡應整合 Polly 等重試機制，處理 429 (Rate Limit) 或 503 錯誤
                var response = await _client.GenerateContentAsync(
                    request ,
                    ct
                );

                // 4. 更新最後紀錄的 Token 數量，供 SessionManager 紀錄日誌使用
                if(response?.UsageMetadata != null)
                {
                    this.LastTotalTokens = response.UsageMetadata.TotalTokenCount;
                }

                return response;
            }
            catch(Exception ex)
            {
                // 記錄錯誤並向上拋出，讓 SessionManager 的 ExceptionUtility 處理扁平化
                LogExceptionWhenSendingMessage(_logger , ex.Message);
                throw;
            }
        }

        /// <inheritdoc cref="SendMessageAsync(GeminiGenerateRequest, GeminiMessage, AiExecutionSettings, CancellationToken)"/>
        /// <remarks>
        /// The clone version of <seealso cref="SendMessageAsync(GeminiGenerateRequest, GeminiMessage, AiExecutionSettings, CancellationToken)"/>
        /// </remarks>
        public async Task<GeminiResponse> WithSendMessageAsync(
            GeminiGenerateRequest request ,
            GeminiMessage message , // 建議改為接收 GeminiMessage 物件，以支援多 Parts (Function Responses)
            AiExecutionSettings settings,
            CancellationToken ct = default            
        )
        {
            // 1. 自動 Token 檢查與壓縮記憶體 (Memory Consolidation)
            // 在發送新請求前，檢查是否需要壓縮過去的對話以節省空間
            var clone = await request.WithConsolidateMemoryAsync(
                _client ,
                LastTotalTokens,
                settings
            );

            // 2. 將新訊息加入請求內容
            clone.AddMessage(message);

            try
            {
                // 3. 呼叫底層 Gemini API 
                // 這裡應整合 Polly 等重試機制，處理 429 (Rate Limit) 或 503 錯誤
                var response = await _client.GenerateContentAsync(
                    request ,
                    ct
                );

                // 4. 更新最後紀錄的 Token 數量，供 SessionManager 紀錄日誌使用
                if(response?.UsageMetadata != null)
                {
                    this.LastTotalTokens = response.UsageMetadata.TotalTokenCount;
                }

                return response;
            }
            catch(Exception ex)
            {
                // 記錄錯誤並向上拋出，讓 SessionManager 的 ExceptionUtility 處理扁平化
                LogExceptionWhenSendingMessage(_logger , ex.Message);
                throw;
            }
        }

        /// <inheritdoc cref="SendMessageAsync(GeminiGenerateRequest, GeminiMessage, AiExecutionSettings, CancellationToken)"/>
        /// <param name="userText">user prompt</param>
        /// <returns></returns>
        public async Task<GeminiResponse> SendMessageAsync(
            GeminiGenerateRequest request ,
            string userText ,
            AiExecutionSettings settings ,
            CancellationToken ct = default
        )
        {
            var message = new GeminiMessage
            {
                Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER , // user
                Parts = new List<GeminiPart> { new GeminiPart { Text = userText } }
            };
            return await SendMessageAsync(
                request ,
                message ,
                settings ,
                ct
            );
        }

        /// <inheritdoc cref="SendMessageAsync(GeminiGenerateRequest, string, AiExecutionSettings, CancellationToken)"/>
        /// <remarks>
        /// The clone version of <seealso cref="SendMessageAsync(GeminiGenerateRequest, string, AiExecutionSettings, CancellationToken)"/> method
        /// </remarks>
        public async Task<GeminiResponse> WithSendMessageAsync(
            GeminiGenerateRequest request ,
            string userText ,
            AiExecutionSettings settings ,
            CancellationToken ct = default
        )
        {
            var message = new GeminiMessage
            {
                Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER , // user
                Parts = new List<GeminiPart> { new GeminiPart { Text = userText } }
            };
            return await WithSendMessageAsync(
                request ,
                message ,
                settings ,
                ct
            );
        }
        /// <inheritdoc cref="SendMessageAsync(GeminiGenerateRequest, GeminiMessage, AiExecutionSettings, CancellationToken)"/>
        /// <param name="userText">user prompt</param>
        /// <returns></returns>
        public async Task<GeminiResponse> SendMessageAsync(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userText ,
            AiExecutionSettings settings ,
            CancellationToken ct = default
        )
        {
            var message = new GeminiMessage
            {
                Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER , // user
                Parts = new List<GeminiPart> { new GeminiPart { RawText = userText } }
            };
            return await SendMessageAsync(
                request ,
                message ,
                settings ,
                ct
            );
        }

        /// <inheritdoc cref="SendMessageAsync(GeminiGenerateRequest, string, AiExecutionSettings, CancellationToken)"/>
        /// <remarks>
        /// The clone version of <seealso cref="SendMessageAsync(GeminiGenerateRequest, string, AiExecutionSettings, CancellationToken)"/> method
        /// </remarks>
        public async Task<GeminiResponse> WithSendMessageAsync(
            GeminiGenerateRequest request ,
            ReadOnlyMemory<char> userText ,
            AiExecutionSettings settings ,
            CancellationToken ct = default
        )
        {
            var message = new GeminiMessage
            {
                Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER , // user
                Parts = new List<GeminiPart> { new GeminiPart { RawText = userText } }
            };
            return await WithSendMessageAsync(
                request ,
                message ,
                settings ,
                ct
            );
        }
    }
}
