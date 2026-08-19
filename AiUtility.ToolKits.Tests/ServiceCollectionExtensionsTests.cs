using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using AiUtility.ToolKits.Extensions;
using AiUtility.ToolKits.Abstractions;
using Xunit;
using ReflectionUtilityServices;
using ExpressionTreeUtilityServices;
using TaskUtilityServices;

namespace AiUtility.ToolKits.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddAiTools_ShouldRegisterServicesInDIContainer()
        {
            // Arrange
            var services = new ServiceCollection();

            // 必須註冊 Dispatcher 依賴的基礎服務
            services.AddSingleton<ITaskUtilityService , TaskUtilityService>();
            services.AddSingleton<IExpressionTreeUtilityService , ExpressionTreeUtilityService>();
            services.AddSingleton<IReflectionUtilityService , ReflectionUtilityService>();

            // Act
            // 修正點：更新 metadataFactory 以符合 ToolMetadataBase 的新建構子 (傳入 attrs)
            services.AddAiTools<TestToolMetadata , TestToolAttribute>((mi , res) =>
            {
                var attrs = mi.GetCustomAttributes<Attribute>();
                return new TestToolMetadata(
                    mi.Name ,
                    mi ,
                    mi.GetParameters() ,
                    (inst , args) => null ,
                    null ,
                    attrs // 傳遞 MethodAttributes
                );
            });

            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var registry = serviceProvider.GetService<IToolRegistry<TestToolMetadata , TestToolAttribute>>();
            var dispatcher = serviceProvider.GetService<IToolDispatcher<TestToolMetadata , TestToolAttribute>>();

            Assert.NotNull(registry);
            Assert.NotNull(dispatcher);
            Assert.IsAssignableFrom<IToolRegistry<TestToolMetadata , TestToolAttribute>>(registry);
            Assert.IsAssignableFrom<IToolDispatcher<TestToolMetadata , TestToolAttribute>>(dispatcher);
        }
    }
}
