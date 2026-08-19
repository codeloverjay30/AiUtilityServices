using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AiUtility.ToolKits.Abstractions
{
    public interface IToolRegistry<TMetadata, TAttribute>
    where TMetadata : ToolMetadataBase
    where TAttribute : Attribute
    {
        void RegisterFromAssemblies(IEnumerable<Assembly> assemblies , Func<Type , object>? serviceResolver = null);
        void RegisterFromAssembly(Assembly assembly , Func<Type , object>? serviceResolver = null);
        void Register<T>(Func<T> factory) where T : class;
        bool TryGetTool(string functionName , out TMetadata? metadata);
        IEnumerable<TMetadata> GetAllTools();
    }
}
