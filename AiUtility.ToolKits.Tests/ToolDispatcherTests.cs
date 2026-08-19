using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Reflection;
using System.Threading.Tasks;
using AiUtility.ToolKits.Dispatcher;
using AiUtility.ToolKits.Registry;
using AiUtility.ToolKits.Abstractions;
using ExpressionTreeUtilityServices;
using ReflectionUtilityServices;
using TaskUtilityServices;
using Xunit;

namespace AiUtility.ToolKits.Tests
{
    public class ToolDispatcherTests
    {
        private readonly ToolRegistry<TestToolMetadata , TestToolAttribute> _registry;
        private readonly ToolDispatcher<TestToolMetadata , TestToolAttribute> _dispatcher;
        private readonly ITaskUtilityService _taskUtilityService;
        private readonly IExpressionTreeUtilityService _expressionTreeUtilityService;
        private readonly IReflectionUtilityService _reflectionUtilityService;

        public ToolDispatcherTests()
        {
            _taskUtilityService = new TaskUtilityService();
            _expressionTreeUtilityService = new ExpressionTreeUtilityService();
            _reflectionUtilityService = new ReflectionUtilityService(_expressionTreeUtilityService);

            // 修正點 1: 更新 metadataFactory 以符合 ToolMetadataBase 的新架構
            _registry = new ToolRegistry<TestToolMetadata , TestToolAttribute>((mi , resolver) =>
            {
                // 註冊時抓取該方法的所有 Attribute (包含 Data Annotations)
                var attrs = mi.GetCustomAttributes<Attribute>();

                // 這裡模擬 ReflectionUtility 產生 FastInvoke
                _reflectionUtilityService.AddFastDelegate(mi);
                var fastInvoke = _reflectionUtilityService.FastDelegates [ _reflectionUtilityService.FastDelegates.Count - 1 ];

                return new TestToolMetadata(
                    mi.Name ,
                    mi ,
                    mi.GetParameters() ,
                    fastInvoke! ,
                    () => new MockApiService() ,
                    attrs // 傳入 MethodAttributes
                );
            });

            // 註冊 Mock 服務
            _registry.Register<MockApiService>(() => new MockApiService());

            // 修正點 2: 初始化 Dispatcher
            _dispatcher = new ToolDispatcher<TestToolMetadata , TestToolAttribute>(
                _registry ,
                _reflectionUtilityService ,
                _taskUtilityService
            );
        }

        [Fact]
        public async Task DispatchAsync_WithSyncMethod_ShouldReturnString()
        {
            // Arrange: 模擬 AI 傳來的 JSON 參數 (符合 IToolDispatcher 介面定義)
            var arguments = new Dictionary<string , JsonElement>
            {
                {
                    AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER, // "user"
                    JsonDocument.Parse("\"Gemini\"").RootElement
                }
            };

            // Act
            var result = await _dispatcher.DispatchAsync("Greet" , arguments);

            // Assert
            Assert.Equal("Hi, Gemini" , result);
        }

        [Fact]
        public async Task DispatchAsync_WithAsyncMethod_ShouldAwaitAndReturnResult()
        {
            // Arrange
            var arguments = new Dictionary<string , JsonElement>
            {
                { "value", JsonDocument.Parse("16").RootElement }
            };

            // Act
            var result = await _dispatcher.DispatchAsync("CalculateSquareRootAsync" , arguments);

            // Assert
            Assert.Equal(4.0 , (double)result!);
        }

        // --- 內部 Mock 類別 ---
        public class MockApiService
        {
            [TestTool]
            public string Greet(string user) => $"Hi, {user}";

            [TestTool]
            public async Task<double> CalculateSquareRootAsync(double value)
            {
                await Task.Delay(10);
                return Math.Sqrt(value);
            }
        }
    }
}
