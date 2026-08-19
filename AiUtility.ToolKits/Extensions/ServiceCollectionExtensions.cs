using AiUtility.ToolKits.Abstractions;
using AiUtility.ToolKits.Dispatcher;
using AiUtility.ToolKits.Registry;
using ExpressionTreeUtilityServices;
using Microsoft.Extensions.DependencyInjection;
using ReflectionUtilityServices;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TaskUtilityServices;

namespace AiUtility.ToolKits.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAiTools<TMetadata, TAttribute>(
            this IServiceCollection services ,
            Func<MethodInfo , Func<Type , object> , TMetadata> metadataFactory)
            where TMetadata : ToolMetadataBase
            where TAttribute : Attribute
        {
            services.AddScoped<ITaskUtilityService,TaskUtilityService>();
            services.AddScoped<IExpressionTreeUtilityService,ExpressionTreeUtilityService>();
            services.AddScoped<IReflectionUtilityService,ReflectionUtilityService>();
            services.AddSingleton<IToolRegistry<TMetadata , TAttribute>>(sp =>
                new ToolRegistry<TMetadata , TAttribute>(metadataFactory));
            services.AddScoped<IToolDispatcher<TMetadata , TAttribute> , ToolDispatcher<TMetadata , TAttribute>>();
            return services;
        }
    }
}
