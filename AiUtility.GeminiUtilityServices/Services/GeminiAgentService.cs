using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Configs;
using AiUtility.GeminiUtilityServices.Models;
using AssemblyUtilityServices;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Services
{
    public partial class GeminiAgentService
        :IGeminiAgentService
    {
        private readonly ILoggerFactoryBaseUtilityService _loggerFactoryService;
        public ILoggerFactoryBaseUtilityService LoggerFactoryService => _loggerFactoryService;
        private ILogger _logger => _loggerFactoryService.Logger;

        [LoggerMessage(Level = LogLevel.Information , Message = "Gemini Responses: {Response}")]
        static partial void LogResponse(ILogger logger , string Response);

        [LoggerMessage(Level = LogLevel.Information , Message = "Will ask to Gemini with response {Response}")]
        static partial void LogNextRound(ILogger logger , string Response);

        /// <summary>
        /// A <see cref="List{T}"/> of <see cref="global::System.Reflection.Assembly"/> to stored the loaded assemblies using <see cref="_assembliesUtilityService"/> service.
        /// </summary>

        private List<Assembly> _assemblies = new();
        public List<Assembly> Assemblies => _assemblies;

        /// <summary>
        /// Assembly service to list all .dlls and load them to <see cref="_assemblies"/>.
        /// </summary>
        private readonly IAssembliesUtilityService _assembliesUtilityService;
        public IAssembliesUtilityService AssembliesUtilityService => _assembliesUtilityService;

        /// <summary>
        /// A request used for Gemini AI Studio, see <seealso cref="GeminiGenerateRequest"/> for more details.
        /// </summary>
        private GeminiGenerateRequest _request;
        public GeminiGenerateRequest Request => _request;

        /// <summary>
        /// Dispatcher
        /// </summary>
        private readonly IGeminiToolDispatcher _dispatcher;
        public IGeminiToolDispatcher Dispatcher => _dispatcher;

        /// <summary>
        /// Tool service to get cached metadata from <see cref="GeminiToolMetadata"/> 
        /// </summary>
        private readonly IGeminiToolService _toolService;
        public IGeminiToolService ToolService => _toolService;

        /// <summary>
        /// Session manager
        /// </summary>
        private readonly IGeminiSessionManager _sessionManager;
        public IGeminiSessionManager SessionManager => _sessionManager;

        /// <summary>
        /// Conversation manager
        /// </summary>
        public IGeminiConversationManager ConversationManager => SessionManager.ConversationManager;

        /// <summary>
        /// A registry to register all methods of a class or
        /// an <see cref="global::System.Reflection.Assembly"/> that
        /// are marked with `[GeminiTool]` Attribute to cached metadata <see cref="GeminiToolMetadata"/>
        /// </summary>
        public IGeminiToolRegistry ToolRegistry => Dispatcher.ToolRegistry;
        private GeminiTool _tool { get; set; }
        public GeminiTool Tool => _tool;

        public GeminiAgentService(
            ILoggerFactoryBaseUtilityService loggerFactoryService ,
            IAssembliesUtilityService assembliesUtilityService ,
            IGeminiToolDispatcher dispatcher ,
            IGeminiSessionManager sessionManager
        )
        {
            _loggerFactoryService = loggerFactoryService;
            _assembliesUtilityService = assembliesUtilityService;
            _dispatcher = dispatcher;
            _sessionManager = sessionManager;
            _request = new GeminiConfig().DefaultRequestConfig;
            this.Configure();
        }


        public void Configure()
        {
            var dllFiles = _assembliesUtilityService.ListAllAssemblies();
            _assemblies = _assembliesUtilityService.LoadAllAssemblies(dllFiles);
            ToolRegistry.RegisterFromAssemblies(_assemblies);
        }
    }
}
