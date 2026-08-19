using AiUtility.ToolKits.Abstractions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using TypeUtilityServices;

namespace AiUtility.ToolKits.Executor
{
    public class AiToolExecutor<TMetadata, TAttribute>(
        IToolRegistry<TMetadata , TAttribute> registry ,
        ITypeUtilityService typeUtilityService
    ) : AiToolExecutorBase<TMetadata , TAttribute>(registry, typeUtilityService)
        where TMetadata : ToolMetadataBase
        where TAttribute : Attribute
    {
        
    }
}
