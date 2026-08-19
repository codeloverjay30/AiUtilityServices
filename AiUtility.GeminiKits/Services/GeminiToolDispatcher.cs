using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Registry;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Services
{
    public class GeminiToolDispatcher : IGeminiToolDispatcher
    {
        private readonly IGeminiToolRegistry _toolRegistry;
        public IGeminiToolRegistry ToolRegistry => _toolRegistry;

        private readonly ITypeUtilityService _typeUtilityService;
        public ITypeUtilityService TypeUtilityService => _typeUtilityService;

        public GeminiToolDispatcher(
            IGeminiToolRegistry toolRegistry,
            ITypeUtilityService typeUtilityService
        )
        {
            _toolRegistry = toolRegistry;
            _typeUtilityService = typeUtilityService;
        }

        /// <summary>
        /// Dispatch the task from registered cached tool and auto execute it.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task<object?> DispatchAsync(
            string functionName , 
            Dictionary<string , object> arguments,
            CancellationToken ct = default
        )
        {
            // 1. 從 Registry 尋找工具 Metadata
            if(!_toolRegistry.TryGetTool(functionName , out var metadata))
            {
                throw new KeyNotFoundException($"[Dispatcher] 找不到名稱為 '{functionName}' 的工具。");
            }

            var parameters = metadata.Parameters;
            object? [ ] paramValues = new object? [ parameters.Length ];

            // 2. 參數對照與 Data Annotations 驗證
            for(int i = 0; i < parameters.Length; i++)
            {
                var param = parameters [ i ];
                arguments.TryGetValue(param.Name! , out var val);
                if (param.ParameterType == typeof(CancellationToken))
                {
                    paramValues[i] = ct;
                    continue;
                }

                // Match argument dictionary to method parameters
                if (arguments.TryGetValue(param.Name!, out var value))
                {
                    // Use ITypeUtilityService to ensure the value matches the expected parameter type
                    paramValues[i] = _typeUtilityService.SafeConvert(value, param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    paramValues[i] = param.DefaultValue;
                }
                else
                {
                    throw new ArgumentException($"Missing required argument: {param.Name}");
                }
                
                // 執行參數等級的驗證 (例如 [Required], [Range], [StringLength])
                var validationAttrs = param.GetCustomAttributes<ValidationAttribute>();
                foreach(var attr in validationAttrs)
                {
                    // 若驗證失敗會拋出 ValidationException
                    attr.Validate(val , param.Name ?? "Parameter");
                }

                paramValues [ i ] = val;
            }

            // 3. 取得 POCO 實例 (取代原有的 _serviceInstances)
            // 如果是靜態方法，fac 會是 null；如果是實例方法，會透過 Registry 註冊的工廠產生實例
            var instance = metadata.InstanceFactory?.Invoke();

            // 4. 高效執行 (使用 FastDelegate)
            // 注意：DispatchAsync 為非同步，若目標方法是 Task，這裡需處理 Await
            var result = metadata.FastInvoke(instance , paramValues);

            if(result is Task task)
            {
                await task.ConfigureAwait(false);
                return task.GetType().GetProperty("Result")?.GetValue(task);
            }

            return result;
        }
    }
}
