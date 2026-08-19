using AiUtility.ToolKits.Abstractions;
using AiUtility.ToolKits.Models;
using EnumUtilityServices;
using JsonUtilityServices;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AiUtility.ToolKits.Services
{
    public abstract class AiToolConverterBase<TAttribute, TDeclaration, TParameters, TProperty>(
        IJsonUtilityService jsonUtilityService ,
        IEnumUtilityService enumUtilityService ,
        string defaultDescription = "No description" ,
        string defaultParameterDescription = "No description"
    ) : IAiToolConverter<TDeclaration>
        where TAttribute : Attribute
        where TDeclaration : AiToolDeclarationBase, new()
        where TParameters : AiParametersBase, new()
        where TProperty : AiParameterPropertyBase, new()
    {
        public virtual TDeclaration ToToolDeclaration(ToolMetadataBase metadata)
        {
            var toolAttr = metadata.MethodAttributes.OfType<TAttribute>().FirstOrDefault();

            var declaration = new TDeclaration
            {
                Name = metadata.FunctionName ,
                Description = GetDescriptionFromAttribute(toolAttr) ?? defaultDescription ,
                Parameters = CreateParameters(metadata)
            };

            return declaration;
        }

        private TParameters CreateParameters(ToolMetadataBase metadata)
        {
            var parameters = new TParameters();

            foreach(var p in metadata.Parameters)
            {
                var property = new TProperty
                {
                    Type = MapToAiSchemaType(p.ParameterType) ,
                    Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? defaultParameterDescription
                };

                // 處理 Enum
                var enumNames = enumUtilityService.GetEnumNames(p.ParameterType);
                if(enumNames.Length > 0) property.Enum = enumNames.ToList();

                parameters.Properties.Add(p.Name! , property);

                // 處理 Required
                if(p.GetCustomAttribute<RequiredAttribute>() != null || !p.IsOptional)
                {
                    parameters.Required.Add(p.Name!);
                }
            }

            return parameters;
        }

        protected virtual string MapToAiSchemaType(Type type)
        {
            return jsonUtilityService.GetJsonType(type);
        }

        protected abstract string? GetDescriptionFromAttribute(TAttribute? attr);
    }
}

