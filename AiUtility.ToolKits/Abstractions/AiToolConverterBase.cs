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
        /// <summary>
        /// Gets the default description used for tool parameters.
        /// </summary>
        protected string DefaultParameterDescription =>
            defaultParameterDescription;

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
                var property =
                    CreateParameterProperty(
                        p);


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

        /// <summary>
        /// Creates the schema property for the specified tool parameter.
        /// </summary>
        /// <param name="parameter">
        /// The tool parameter metadata.
        /// </param>
        /// <returns>
        /// The generated parameter schema property.
        /// </returns>
        protected virtual TProperty CreateParameterProperty(
            ParameterInfo parameter)
        {
            ArgumentNullException.ThrowIfNull(
                parameter);

            return new TProperty
            {
                Type =
                    MapToAiSchemaType(
                        parameter.ParameterType),

                Description =
                    parameter
                        .GetCustomAttribute<DescriptionAttribute>()
                        ?.Description
                    ?? defaultParameterDescription
            };
        }

        protected virtual string MapToAiSchemaType(Type type)
        {
            return jsonUtilityService.GetJsonType(type);
        }

        protected abstract string? GetDescriptionFromAttribute(TAttribute? attr);
    }
}

