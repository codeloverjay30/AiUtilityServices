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

        private readonly ConcurrentDictionary<Type , object> _cache = new();
        public ConcurrentDictionary<Type , object> Cache => _cache;
        public object Generate<T>() => Generate(typeof(T));
        public object Generate(Type type)
        {
            return _cache.GetOrAdd(type , t => {
                return _InternalGenerate(t);
            });
        }

        internal object _InternalGenerate(Type type)
        {
            string typeStr = _jsonUtilityService.GetJsonType(type);
            if(!typeStr.Equals(TypeConstants.OBJECT)) // "object"
            {
                return new { type = typeStr };
            }

            if(typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
            {
                return new
                {
                    type = TypeConstants.ARRAY, // "array"
                    items = _InternalGenerate(type.GetGenericArguments() [ 0 ])
                };
            }

            var properties = new Dictionary<string , object>();
            var required = new List<string>();

            // iterate all properties that are public and non-static without `[System.Text.Json.Serialization.JsonIgnore]`.

            var publicInstanceProperties =
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() == null);

            foreach(var prop in publicInstanceProperties)
            {
                var propName = prop.Name.ToLower();
                var propSchema = _InternalGenerate(prop.PropertyType);

                var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                if(descriptionAttr != null)
                {
                    propSchema = AddDescriptionToSchema(propSchema , descriptionAttr.Description);
                }

                properties.Add(propName , _InternalGenerate(prop.PropertyType));

                if(!_typeUtilityService.IsNullableType(prop.PropertyType))
                {
                    required.Add(propName);
                }
            }

            return new
            {
                type = TypeConstants.OBJECT , // "object"
                properties = properties ,
                required = required.Count > 0 ? required : null
            };
        }
        private object AddDescriptionToSchema(object schema , string description)
        {
            var dynamicSchema = schema as dynamic;

            var typeProp = GetProperty(schema , AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.FunctionParameters.TYPE); // "type"
            var itemsProp = GetProperty(schema , AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.FunctionParameters.ITEMS); // "items"
            var propsProp = GetProperty(schema , AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.FunctionParameters.PROPERTIES); // "properties"
            var reqProp = GetProperty(schema , AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.FunctionParameters.REQUIRED); //"required"

            return new
            {
                type = typeProp ,
                description = description ,
                items = itemsProp ,
                properties = propsProp ,
                required = reqProp
            };
        }

        internal object? GetProperty(object schema,string propertyName)
        {
            return schema.GetType().GetProperty(propertyName)?.GetValue(schema);
        }
    }
}
