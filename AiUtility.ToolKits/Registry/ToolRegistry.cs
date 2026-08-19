using AiUtility.ToolKits.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AiUtility.ToolKits.Registry
{
    public class ToolRegistry<TMetadata, TAttribute> : IToolRegistry<TMetadata , TAttribute>
        where TMetadata : ToolMetadataBase
        where TAttribute : Attribute
    {
        private readonly ConcurrentDictionary<string , TMetadata> _toolCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Func<MethodInfo , Func<Type , object>? , TMetadata> _metadataFactory;

        public ToolRegistry(Func<MethodInfo , Func<Type , object>? , TMetadata> metadataFactory)
        {
            _metadataFactory = metadataFactory;
        }

        /// <summary>
        /// Register all <see cref="public"/> methods that are marked with <see cref="TAttribute"/> (inherits <see cref="Attribute"/> ) from <paramref name="assemblies"/> to metadata with behaviour <paramref name="serviceResolver"/>.
        /// </summary>
        /// <param name="assemblies"></param>
        /// <param name="serviceResolver"></param>
        public void RegisterFromAssemblies(IEnumerable<Assembly> assemblies , Func<Type , object>? serviceResolver = null)
        {
            foreach(var assembly in assemblies)
            {
                RegisterFromAssembly(assembly , serviceResolver);
            }
        }

        /// <summary>
        /// Register all <see cref="public"/> methods that are marked with <see cref="TAttribute"/> (inherits <see cref="Attribute"/> ) from <paramref name="assembly"/> to metadata with behaviour <paramref name="serviceResolver"/>.
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="serviceResolver"></param>
        /// <remarks>
        /// 1. To use the default behavior, simply pass `null` for the `serviceResolver` parameter.
        /// </remarks>

        public void RegisterFromAssembly(Assembly assembly , Func<Type , object>? serviceResolver = null)
        {
            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Where(m => m.GetCustomAttributes(typeof(TAttribute) , false).Any());

            foreach(var method in methods)
            {
                var metadata = CreateMetadata(method , serviceResolver);
                _toolCache [ metadata.FunctionName ] = metadata;
            }
        }

        /// <summary>
        /// Register all <see cref="public"/> methods that are marked with <see cref="TAttribute"/> (inherits <see cref="Attribute"/> ) by <paramref name="factory"/> <see cref="Func{T}"/> that related to class (e.g. `() => new AnswerService()` ) to metadata 
        ///
        /// This is useful when the tool metadata needs to be created with some custom logic or dependencies that are not easily resolved through reflection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory"></param>

        public void Register<T>(Func<T> factory) where T : class
        {
            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(TAttribute) , false).Any());

            foreach(var method in methods)
            {
                var metadata = CreateMetadata(method , _ => factory());
                _toolCache [ metadata.FunctionName ] = metadata;
            }
        }

        public bool TryGetTool(string functionName , out TMetadata? metadata)
            => _toolCache.TryGetValue(functionName , out metadata);
        public IEnumerable<TMetadata> GetAllTools() => _toolCache.Values;

        protected TMetadata CreateMetadata(MethodInfo method , Func<Type , object>? serviceResolver)
        {
            return _metadataFactory(method , serviceResolver);
        }

    }
}
