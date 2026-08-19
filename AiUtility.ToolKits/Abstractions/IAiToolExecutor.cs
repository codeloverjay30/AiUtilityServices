using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.ToolKits.Abstractions
{
    public interface IAiToolExecutor<TMetadata, TAttribute>
        where TMetadata : ToolMetadataBase
        where TAttribute : Attribute
    {
        Task<object?> ExecuteAsync(
            string functionName , 
            IDictionary<string , object> arguments,
            CancellationToken ct = default
        );
    }
}
