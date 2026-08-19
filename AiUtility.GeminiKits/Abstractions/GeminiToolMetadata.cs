using AiUtility.ToolKits.Abstractions;
using System.Reflection;

namespace AiUtility.GeminiKits.Abstractions
{
    public record GeminiToolMetadata : ToolMetadataBase
    {
        public GeminiToolMetadata(
            string name ,
            MethodInfo mi ,
            ParameterInfo [ ] p ,
            Func<object? , object? [ ]? , object?> fi ,
            Func<object>? fac ,
            IEnumerable<Attribute> methodAttrs
        ) : base(name , mi , p , fi , fac , methodAttrs) { }
    }
}
