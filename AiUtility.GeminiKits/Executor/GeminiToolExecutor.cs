/*

using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Attributes;
using AiUtility.ToolKits.Abstractions;
using AiUtility.ToolKits.Executor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TypeUtilityServices;


namespace AiUtility.GeminiKits.Executor
{
    public class GeminiToolExecutor(
        IToolRegistry<GeminiToolMetadata, GeminiToolAttribute> registry,
        ITypeUtilityService typeUtilityService
    ) : AiToolExecutor<GeminiToolMetadata, GeminiToolAttribute>(registry, typeUtilityService),
        IGeminiToolExecutor
    {
        public async Task<object?> ExecuteAsync(
            string functionName,
            IDictionary<string, object> arguments,
            CancellationToken ct = default
        )
        {
            // 1. Find the tool metadata by the function name
            var isSuccess = registry.TryGetTool(functionName,out var tool);
            if (tool == null)
            {
                throw new KeyNotFoundException($"Tool with name '{functionName}' was not found in the registry.");
            }

            // 2. Prepare the parameters for the method call
            var method = tool.MethodInfo;
            var parameters = method.GetParameters();
            var args = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                
                // Handle CancellationToken if requested
                if (param.ParameterType == typeof(CancellationToken))
                {
                    args[i] = ct;
                    continue;
                }

                // Match argument dictionary to method parameters
                if (arguments.TryGetValue(param.Name!, out var value))
                {
                    // Use typeUtilityService to ensure the value matches the expected parameter type
                    args[i] = typeUtilityService.SafeConvert(value, param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    throw new ArgumentException($"Missing required argument: {param.Name}");
                }
            }

            // 執行方法
            var result = tool.FastInvoke(tool.InstanceFactory?.Invoke(), args);

            // 4. Handle Task/ValueTask return types for async methods
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                
                // Extract result if it's a Task<T>
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            return result;
        }
    }
}
*/