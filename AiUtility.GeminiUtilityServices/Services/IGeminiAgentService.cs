using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AssemblyUtilityServices;
using LoggerFactoryUtilityServices;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Services
{
    public interface IGeminiAgentService
    {
        ILoggerFactoryBaseUtilityService LoggerFactoryService { get; }
        List <Assembly> Assemblies { get; }
        IAssembliesUtilityService AssembliesUtilityService { get; }


        GeminiGenerateRequest Request { get; }
        IGeminiToolDispatcher Dispatcher { get; }
        IGeminiToolService ToolService { get; }
        IGeminiConversationManager ConversationManager { get; }
        IGeminiToolRegistry ToolRegistry { get; }
        GeminiTool Tool { get; }

        void Configure();
    }
}
