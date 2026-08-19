using AiUtility.ToolKits.Abstractions;
using ReflectionUtilityServices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TaskUtilityServices;

namespace AiUtility.ToolKits.Dispatcher
{
    public class ToolDispatcher<TMetadata, TAttribute>(
        IToolRegistry<TMetadata , TAttribute> registry,
        IReflectionUtilityService reflectionUtilityService,
        ITaskUtilityService taskUtilityService
    ): IToolDispatcher<TMetadata , TAttribute>
        where TMetadata : ToolMetadataBase
        where TAttribute : Attribute
    {
        private readonly IReflectionUtilityService _reflectionUtilityService = reflectionUtilityService;
        private readonly ITaskUtilityService _taskUtilityService = taskUtilityService;
        public async Task<object?> DispatchAsync(
            string functionName , 
            Dictionary<string , JsonElement> arguments
        )
        {
            if(!registry.TryGetTool(functionName , out var tool))
            {
                throw new KeyNotFoundException($"Function {functionName} not found.");
            }
            // tool 現在是 TMetadata 型別，具有 ToolMetadataBase 的屬性
            var args = _reflectionUtilityService.BindArguments(tool!.Parameters , arguments);
            var instance = tool.InstanceFactory?.Invoke();

            var result = tool.FastInvoke(instance , args);

            return await _taskUtilityService.HandleAsyncResult(result);
        }
    }
}
