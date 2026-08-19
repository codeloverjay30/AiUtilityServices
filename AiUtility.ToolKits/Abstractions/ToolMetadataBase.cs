using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AiUtility.ToolKits.Abstractions
{

    /// <summary>
    /// Base of metadata for tools about AI.
    /// </summary>
    /// <param name="FunctionName">function name</param>
    /// <param name="Method">method info</param>
    /// <param name="Parameters">parameters info of <paramref name="Method"/></param>
    /// <param name="FastInvoke">The cached delegate that is reflected given <paramref name="FunctionName"/>,<paramref name="Method"/>, and <paramref name="Parameters"/></param>
    /// <param name="InstanceFactory">factory method to create instance</param>
    public abstract record ToolMetadataBase
    {
        /// <summary>
        /// function name
        /// </summary>
        public string FunctionName { get; init; }
        /// <summary>
        /// method info
        /// </summary>
        public MethodInfo MethodInfo { get; init; }
        /// <summary>
        /// parameters info of <see cref="MethodInfo"/>
        /// </summary>
        public ParameterInfo [ ] Parameters { get; init; }

        /// <summary>
        /// The cached delegate that is reflected given <see cref="FunctionName"/>,<see cref="Method"/>, and <see cref="Parameters"/>
        /// </summary>
        public Func<object? , object? [ ]? , object?> FastInvoke { get; init; }

        /// <summary>
        /// factory method to create instance
        /// </summary>
        public Func<object>? InstanceFactory { get; init; }

        /// <summary>
        /// A readonly list to manage all Attributes (marked in Data Annotation) of a method
        /// </summary>
        public IReadOnlyList<Attribute> MethodAttributes { get; init; }

        protected ToolMetadataBase(
            string name ,
            MethodInfo mi ,
            ParameterInfo [ ] p ,
            Func<object? , object? [ ]? , object?> fi ,
            Func<object>? fac ,
            IEnumerable<Attribute> methodAttrs
        )
        {
            FunctionName = name;
            MethodInfo = mi;
            Parameters = p;
            FastInvoke = fi;
            InstanceFactory = fac;
            MethodAttributes = methodAttrs.ToList().AsReadOnly();
        }
    }
}

