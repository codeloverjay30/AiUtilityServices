using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Attributes;
using AiUtility.ToolKits.Registry;
using ReflectionUtilityServices;

namespace AiUtility.GeminiKits.Registry
{
    public class GeminiToolRegistry : DefaultToolRegistry<GeminiToolMetadata , GeminiToolAttribute>, IGeminiToolRegistry
    {
        public GeminiToolRegistry(IReflectionUtilityService reflectionService)
            : base(reflectionService , (method , resolver , attrs) =>
                new GeminiToolMetadata(
                    method.Name ,
                    method ,
                    method.GetParameters() ,
                    reflectionService.FastInvoke!,
                    method.IsStatic ? null : () => resolver!(method.DeclaringType!) ,
                    attrs
                ))
        { }
    }
}
