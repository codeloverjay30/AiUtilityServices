using AiUtility.AiBaseUtilityServices.Services;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.DataAnnotations;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.ToolKits.Abstractions;
using JsonUtilityServices;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static AiUtility.GeminiUtilityServices.Models.GeminiGenerateRequest;

namespace AiUtility.GeminiUtilityServices.Services
{
    /// <summary>
    /// Synchronizes registered Gemini tools to generation requests.
    /// </summary>
    public partial class GeminiToolService(
        IGeminiToolRegistry registry ,
        IAiToolConverter<object> converter,
        ILoggerFactoryBaseUtilityService loggerFactoryService ,
        bool toLogWhenSuccess
    ) :
        AiBaseUtilityService(
            loggerFactoryService ,
            toLogWhenSuccess
        ), IGeminiToolService
    {
        private readonly ILogger _logger = loggerFactoryService.Logger;

        /// <summary>
        /// Logs the number of Gemini tool declarations synchronized to a request.
        /// </summary>
        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Synchronizing Gemini tools. ToolCount={ToolCount}")]
        private static partial void LogGeminiToolCount(
            ILogger logger,
            int toolCount);

        /// <summary>
        /// Logs when no Gemini tools are available for the request.
        /// </summary>
        [LoggerMessage(
            Level = LogLevel.Warning,
            Message =
                "No Gemini tools are registered. Gemini cannot perform tool execution.")]
        private static partial void LogNoGeminiTools(ILogger logger);

        /// <summary>
        /// Synchronizes all registered tool declarations to the Gemini request.
        /// </summary>
        /// <param name="request">The Gemini request to update.</param>
        public void SyncToolsToRequest(GeminiGenerateRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var declarations = registry.GetAllTools()
                .Select(metadata => converter.ToToolDeclaration(metadata))
                .ToList();

            LogGeminiToolCount(_logger, declarations.Count);

            if (declarations.Count == 0)
            {
                LogNoGeminiTools(_logger);
                request.Tools = [];
                return;
            }

            if(declarations.Any())
            {
                request.Tools = new List<GeminiToolDeclarationWrapper>
                {
                    new GeminiToolDeclarationWrapper { FunctionDeclarations = declarations
                        .Select(declaration =>
                            (AiUtility.GeminiKits.Models.GeminiToolDeclaration)declaration)
                        .ToList() }
                };
            }
        }
    }
}
