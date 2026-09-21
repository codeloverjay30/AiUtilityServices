extern alias MimeTypeAlias;
extern alias TypeAlias;

using MimeTypes = MimeTypeAlias::CommonConstants.MimeTypes;
using TypeConstants = TypeAlias::CommonConstants.Types.TypeConstants;

using JsonUtilityServices;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TypeUtilityServices;
using System.Text.Json.Serialization;
using AiUtility.ToolKits.Models;

namespace AiUtility.GeminiUtilityServices.Services
{
    public class GeminiSchemaGenerator(
        IJsonUtilityService jsonUtilityService ,
        ITypeUtilityService typeUtilityService
    ) : IGeminiSchemaGenerator
    {
        /// <summary>
        /// Json Utility Service
        /// </summary>
        private readonly IJsonUtilityService _jsonUtilityService = jsonUtilityService;
        public IJsonUtilityService JsonUtilityServices => _jsonUtilityService;

        /// <summary>
        /// Type utility service
        /// </summary>
        private readonly ITypeUtilityService _typeUtilityService = typeUtilityService;
        public ITypeUtilityService TypeUtilityServices => _typeUtilityService;

    private readonly ConcurrentDictionary<Type,AiParameterPropertyBase> _cache =
        new();

        public ConcurrentDictionary<
            Type,
            AiParameterPropertyBase> Cache =>
                _cache;

        /// <summary>
/// Generates or retrieves a cached schema for the specified CLR type.
/// </summary>
/// <typeparam name="T">
/// The CLR type to generate a schema for.
/// </typeparam>
/// <returns>
/// The generated schema.
/// </returns>
public AiParameterPropertyBase Generate<T>()
{
    return Generate(
        typeof(T));
}

        /// <summary>
        /// Generates or retrieves a cached schema for the specified CLR type.
        /// </summary>
        /// <param name="type">
        /// The CLR type to generate a schema for.
        /// </param>
        /// <returns>
        /// The generated schema.
        /// </returns>
        public AiParameterPropertyBase Generate(
            Type type)
        {
            ArgumentNullException.ThrowIfNull(
                type);

            return _cache.GetOrAdd(
                type,
                _InternalGenerate);
        }


        /// <summary>
        /// Generates a Gemini-compatible schema for the specified CLR type.
        /// </summary>
        /// <param name="type">
        /// The CLR type to convert to a Gemini schema.
        /// </param>
        /// <returns>
        /// The generated Gemini-compatible schema.
        /// </returns>
        internal AiParameterPropertyBase _InternalGenerate(
            Type type)
        {
            ArgumentNullException.ThrowIfNull(
                type);

            if (_typeUtilityService.TryGetCollectionElementType(
                    type,
                    out var elementType))
            {
                if (elementType is null)
                {
                    throw new InvalidOperationException(
                        $"The collection element type could not be determined for '{type.FullName}'.");
                }

                return new AiParameterPropertyBase
                {
                    Type =
                        TypeConstants.ARRAY,

                    Items =
                        _InternalGenerate(
                            elementType)
                };
            }

            if (_typeUtilityService.IsComplexType(
                    type))
            {
                return GenerateObjectSchema(
                    type);
            }

            return new AiParameterPropertyBase
            {
                Type =
                    _jsonUtilityService.GetJsonType(
                        type)
            };
        }


        /// <summary>
        /// Generates a Gemini object schema for the specified complex CLR type.
        /// </summary>
        /// <param name="type">
        /// The complex CLR type to inspect.
        /// </param>
        /// <returns>
        /// The generated Gemini object schema.
        /// </returns>
        private AiParameterPropertyBase GenerateObjectSchema(
            Type type)
        {
            ArgumentNullException.ThrowIfNull(
                type);

            var properties =
                new Dictionary<
                    string,
                    AiParameterPropertyBase>();

            var required =
                new List<string>();

            var publicInstanceProperties =
                type.GetProperties(
                        BindingFlags.Public |
                        BindingFlags.Instance)
                    .Where(
                        property =>
                            property.GetCustomAttribute<
                                JsonIgnoreAttribute>()
                            is null);

            foreach (var property in
                     publicInstanceProperties)
            {
                var propertyName =
                    property.Name.ToLowerInvariant();

                var propertySchema =
                    _InternalGenerate(
                        property.PropertyType);

                var descriptionAttribute =
                    property.GetCustomAttribute<
                        DescriptionAttribute>();

                if (descriptionAttribute is not null)
                {
                    propertySchema.Description =
                        descriptionAttribute.Description;
                }

                properties.Add(
                    propertyName,
                    propertySchema);

                if (!_typeUtilityService.IsNullableType(
                        property.PropertyType))
                {
                    required.Add(
                        propertyName);
                }
            }

            return new AiParameterPropertyBase
            {
                Type =
                    TypeConstants.OBJECT,

                Properties =
                    properties,

                Required =
                    required.Count > 0
                        ? required
                        : null
            };
        }

        internal object? GetProperty(object schema,string propertyName)
        {
            return schema.GetType().GetProperty(propertyName)?.GetValue(schema);
        }
    }
}
