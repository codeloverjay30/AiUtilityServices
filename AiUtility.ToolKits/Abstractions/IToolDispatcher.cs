using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AiUtility.ToolKits.Abstractions
{
    public interface IToolDispatcher<TMetadata, TAttribute>
    {
        Task<object?> DispatchAsync(string functionName , Dictionary<string , JsonElement> arguments);
    }
}
