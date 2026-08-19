using AiUtility.GeminiKits.Attributes;
using AiUtility.ToolKits.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiKits.Abstractions
{
    public interface IGeminiToolExecutor : IAiToolExecutor<GeminiToolMetadata, GeminiToolAttribute>
    {
    }
}
