using AiUtility.ToolKits.Abstractions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using TypeUtilityServices;

namespace AiUtility.ToolKits.Executor
{
    public abstract class AiToolExecutorBase<TMetadata, TAttribute>(
        IToolRegistry<TMetadata , TAttribute> registry ,
        ITypeUtilityService typeUtilityService
    ) : IAiToolExecutor<TMetadata , TAttribute>
        where TMetadata : ToolMetadataBase
        where TAttribute : Attribute
    {
        public virtual async Task<object?> ExecuteAsync(
            string functionName ,
            IDictionary<string , object> arguments,
            CancellationToken ct = default
        )
        {
            // 1. 查表 (利用您在 ToolRegistry 實作好的 TryGetTool)
            if(!registry.TryGetTool(functionName , out var metadata) || metadata == null)
                throw new KeyNotFoundException($"Tool {functionName} not found.");

            // 2. 轉換參數 (這部分可以拆成 protected virtual 讓子類別能自定義)
            var invokeArgs = PrepareArgs(metadata , arguments);

            // 3. 執行
            var instance = metadata.InstanceFactory?.Invoke();
            return metadata.FastInvoke(instance , invokeArgs);
        }

        protected virtual object? [ ] PrepareArgs(TMetadata metadata , IDictionary<string , object> arguments)
        {
            var parameters = metadata.MethodInfo.GetParameters();
            var args = new object? [ parameters.Length ];
            for(int i = 0; i < parameters.Length; i++)
            {
                var p = parameters [ i ];
                if(arguments.TryGetValue(p.Name! , out var val))
                    args [ i ] = typeUtilityService.SafeConvert(val , p.ParameterType);
                else if(p.HasDefaultValue)
                    args [ i ] = p.DefaultValue;
            }
            return args;
        }
    }
}
