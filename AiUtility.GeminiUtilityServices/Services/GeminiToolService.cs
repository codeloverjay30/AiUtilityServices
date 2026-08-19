using AiUtility.AiBaseUtilityServices.Services;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.DataAnnotations;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.ToolKits.Abstractions;
using JsonUtilityServices;
using LoggerFactoryUtilityServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static AiUtility.GeminiUtilityServices.Models.GeminiGenerateRequest;

namespace AiUtility.GeminiUtilityServices.Services
{
    public class GeminiToolService(
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
        public void SyncToolsToRequest(GeminiGenerateRequest request)
        {
            var declarations = registry.GetAllTools()
                .Select(metadata => converter.ToToolDeclaration(metadata))
                .ToList();

            if(declarations.Any())
            {
                request.Tools = new List<GeminiToolDeclarationWrapper>
                {
                    new GeminiToolDeclarationWrapper { FunctionDeclarations = declarations }
                };
            }
        }
    }
}
