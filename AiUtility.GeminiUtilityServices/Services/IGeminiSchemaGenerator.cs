using AiUtility.ToolKits.Models;
using AiUtility.ToolKits.Services;
using JsonUtilityServices;
using System;
using System.Collections.Concurrent;
using TypeUtilityServices;

namespace AiUtility.GeminiUtilityServices.Services
{
    /// <summary>
    /// Defines a service for generating Gemini-compatible schemas
    /// from CLR types.
    /// </summary>
    public interface IGeminiSchemaGenerator : IAiParameterSchemaGenerator
    {
        IJsonUtilityService JsonUtilityServices { get; }

        ITypeUtilityService TypeUtilityServices { get; }

        ConcurrentDictionary<Type, AiParameterPropertyBase> Cache { get; }
    }
}