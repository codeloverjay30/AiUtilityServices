using AiUtility.GeminiKits.Registry;
using System;
using System.Collections.Generic;
using System.Text;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Abstractions
{
    public interface IGeminiToolDispatcher
    {
        IGeminiToolRegistry ToolRegistry { get; }
        ITypeUtilityService TypeUtilityService { get; }
        Task<object?> DispatchAsync(
            string functionName , 
            Dictionary<string , object> arguments,
            CancellationToken ct = default
        );
    }
}
