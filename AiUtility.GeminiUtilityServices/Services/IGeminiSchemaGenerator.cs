using JsonUtilityServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using TypeUtilityServices;

namespace AiUtility.GeminiUtilityServices.Services
{
    public interface IGeminiSchemaGenerator
    {
        public IJsonUtilityService JsonUtilityServices { get; }
        public ITypeUtilityService TypeUtilityServices { get; }
        public ConcurrentDictionary<Type , object> Cache { get; }
        object Generate<T>();
        object Generate(Type type);
    }
}
