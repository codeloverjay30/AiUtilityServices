extern alias MimeTypeAlias;
extern alias TypeAlias;

using MimeTypes = MimeTypeAlias::CommonConstants.MimeTypes;
using TypeConstants = TypeAlias::CommonConstants.Types.TypeConstants;

using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Services;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Validators;
using CustomDataAnnotations.Maintenance;
using FluentValidation;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiUtility.GeminiUtilityServices.Configs;
using AiUtility.GeminiKits.Models;

namespace AiUtility.GeminiUtilityServices.Services
{
    public partial class GeminiApiClient(
        ILoggerFactoryBaseUtilityService loggerFactoryService,
        bool toLogWhenSuccess,
        JsonSerializerOptions? jsonOptions = null
    ):AiBaseAbstractService(
        loggerFactoryService,
        toLogWhenSuccess
    ), IGeminiApiClient
    {
        private readonly JsonSerializerOptions _options =
            jsonOptions ?? AiUtility.Common.Options.JsonOptions.DefaultOptions;

        private ILogger _logger => _loggerFactoryService.Logger;

        [LoggerMessage(Level = LogLevel.Error, Message = "An exception occured at `GenerateContentAsync` method!!! {ErrorDescription}!!! Error message: {ErrorMessage}")]
        static partial void LogExcpetionWhenGeneratingContent(ILogger logger, string ErrorDescription, string ErrorMessage);

        /// <summary>
        /// Logs the serialized Gemini API request payload for diagnostic purposes.
        /// </summary>
        /// <param name="logger">
        /// The logger instance.
        /// </param>
        /// <param name="payload">
        /// The serialized Gemini API request payload.
        /// </param>
        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Gemini API request payload: {Payload}")]
        private static partial void LogGeminiRequestPayload(
            ILogger logger,
            string payload);

        /// <summary>
        /// Logs the runtime assemblies used by the Gemini API client and tool declaration.
        /// </summary>
        /// <param name="logger">
        /// The logger instance.
        /// </param>
        /// <param name="apiClientAssembly">
        /// The Gemini API client assembly identity.
        /// </param>
        /// <param name="apiClientLocation">
        /// The Gemini API client assembly location.
        /// </param>
        /// <param name="toolAssembly">
        /// The Gemini tool declaration assembly identity.
        /// </param>
        /// <param name="toolLocation">
        /// The Gemini tool declaration assembly location.
        /// </param>
        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Gemini runtime assemblies. " +
                "ApiClientAssembly={ApiClientAssembly}, " +
                "ApiClientLocation={ApiClientLocation}, " +
                "ToolAssembly={ToolAssembly}, " +
                "ToolLocation={ToolLocation}")]
        private static partial void LogGeminiRuntimeAssembly(
            ILogger logger,
            string apiClientAssembly,
            string apiClientLocation,
            string toolAssembly,
            string toolLocation);


        public required HttpClient HttpClient { get; init; }
        public required string ApiKey { get; init; } = string.Empty;
        public required GeminiApiOptions ApiOptions { get; init; }

        public string BaseUrl
        {
            get
            {
                ArgumentNullException.ThrowIfNull(ApiOptions);
                ArgumentException.ThrowIfNullOrWhiteSpace(ApiOptions.Model);
                ArgumentException.ThrowIfNullOrWhiteSpace(ApiOptions.ApiVersion);

                return new Uri(
                    ApiOptions.BaseAddress,
                    $"{ApiOptions.ApiVersion}/models/{Uri.EscapeDataString(ApiOptions.Model)}:generateContent")
                    .AbsoluteUri;
            }
        }

        public string RequestUrl => $"{BaseUrl}?key={ApiKey}";

        /// <summary>
        /// Generate content using Gemini API (Google AI Studio)
        /// </summary>
        /// <param name="request"><seealso cref="GeminiGenerateRequest"/></param>
        /// <returns></returns>
        public async Task<GeminiResponse> GenerateContentAsync(
            GeminiGenerateRequest request
        )
        {
            await ValidateRequestAsync(request , new GeminiGenerateRequestValidator());

            LogGeminiRuntimeAssembly(
                _logger,
                typeof(GeminiApiClient)
                    .Assembly
                    .FullName
                    ?? string.Empty,
                typeof(GeminiApiClient)
                    .Assembly
                    .Location,
                typeof(GeminiToolDeclaration)
                    .Assembly
                    .FullName
                    ?? string.Empty,
                typeof(GeminiToolDeclaration)
                    .Assembly
                    .Location);

            var apiPayload = request.ToGoogleApiRequest();
            var json = JsonSerializer.Serialize(apiPayload, _options);

            LogGeminiRequestPayload(
                _logger,
                json);

            var content = new StringContent(
                json ,
                Encoding.UTF8 ,
                MimeTypes.MimeTypeConstants.APPLICATION_JSON // "application/json"
            );

            var response = await HttpClient.PostAsync(RequestUrl , content);

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if(!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{AiUtility.AiBaseUtilityServices.Consts.Constants.Messages.FailureMessages.RUNTIME_EXCEPTION_OCCURRED} {AiUtility.AiBaseUtilityServices.Consts.Constants.AiModels.GEMINI_API} {AiUtility.AiBaseUtilityServices.Consts.Constants.AiModels.REQUEST} {AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionStatus.FAILED}: {response.StatusCode}, {AiUtility.AiBaseUtilityServices.Consts.Constants.AiModels.CONTENT}: {jsonResponse}");
            }

            // 直接反序列化為強型別物件
            var result = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse , _options);

            // throw FormatException indicating a parse exception occured when parsing json data.
            return result ?? throw new FormatException(AiUtility.AiBaseUtilityServices.Consts.Constants.Messages.FailureMessages.AI_API_RUNTIME_PARSE_EXCEPTION);
        }

        public async Task<GeminiResponse> GenerateContentAsync(
            GeminiGenerateRequest request ,
            CancellationToken ct = default
        )
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                return await GenerateContentAsync(request);
            }
            catch(HttpRequestException ex)
            {
                LogExcpetionWhenGeneratingContent(_logger , $"Encounters an {AiUtility.AiBaseUtilityServices.Consts.Constants.Vocabulary.EXCEPTION} during a {AiUtility.AiBaseUtilityServices.Consts.Constants.Protocols.Network.DEFAULT} {AiUtility.AiBaseUtilityServices.Consts.Constants.AiModels.REQUEST}" , ex.Message);
                throw;
            }
            catch(OperationCanceledException ex)
            {
                LogExcpetionWhenGeneratingContent(_logger , $"{AiUtility.AiBaseUtilityServices.Consts.Constants.AiModels.TOKEN} is {AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionStatus.CANCELLED}" , ex.Message);
                throw;
            }
            catch(Exception ex)
            {
                LogExcpetionWhenGeneratingContent(_logger , "Unknown action" , ex.Message);
                throw;
            }
        }
    }
}
