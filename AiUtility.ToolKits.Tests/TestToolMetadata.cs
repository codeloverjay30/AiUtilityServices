using AiUtility.GeminiKits.Attributes;
using AiUtility.ToolKits.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AiUtility.ToolKits.Tests
{
    public record TestToolMetadata : ToolMetadataBase
    {
        public TestToolMetadata(
            string name ,
            MethodInfo mi ,
            ParameterInfo [ ] p ,
            Func<object? , object? [ ]? , object?> fi ,
            Func<object>? fac ,
            IEnumerable<Attribute> methodAttrs
        ): base(name , mi , p , fi , fac , methodAttrs) { }
    }
}
