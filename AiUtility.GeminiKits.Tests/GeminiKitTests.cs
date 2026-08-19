using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Models;
using AiUtility.GeminiKits.Registry;
using AiUtility.GeminiKits.Services;
using EnumUtilityServices;
using ExpressionTreeUtilityServices;
using JsonUtilityServices;
using ReflectionUtilityServices;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TypeUtilityServices;
using Xunit.Abstractions;

namespace AiUtility.GeminiKits.Tests
{
    public class GeminiKitTests
    {
        private IExpressionTreeUtilityService _expressionTreeUtilityService;
        private IReflectionUtilityService _reflectionUtilityService;
        private IEnumUtilityService _enumUtilityService;
        private ITypeUtilityService _typeUtilityService;
        private IJsonUtilityService _jsonUtilityService;

        private GeminiToolRegistry _geminiToolRegistry;
        private GeminiToolConverter _geminiToolConverter;
        private GeminiToolDispatcher _geminiToolDispatcher;
        //private GeminiToolExecutor _geminiToolExecutor;
        private readonly ITestOutputHelper _output;
        public GeminiKitTests(ITestOutputHelper output)
        {
            // 這裡可以進行一些測試前的初始化工作
            _output = output;
            Setup();
        }

        private void Setup()
        {
            ConfigureOtherServices();
            ConfigureAiServices();
        }

        private void ConfigureOtherServices()
        {
            SetupExpressionTreeUtilityService();
            SetupReflectionUtilityService();
            SetupEnumUtilityService();
            SetupTypeUtilityService();
            SetupJsonUtilityService();
        }

        private void ConfigureAiServices()
        {
            SetupRegistry();
            SetupToolConverter();
            SetupDispatcher();
            // SetupToolExecutor();
        }

        private IExpressionTreeUtilityService CreateExpressionTreeUtilityService()
        {
            return new ExpressionTreeUtilityService();
        }
        private void SetupExpressionTreeUtilityService()
        {
            _expressionTreeUtilityService = CreateExpressionTreeUtilityService();
        }
        private IReflectionUtilityService CreateReflectionUtilityService()
        {
            return new ReflectionUtilityService(_expressionTreeUtilityService);
        }
        private void SetupReflectionUtilityService()
        {
            _reflectionUtilityService = CreateReflectionUtilityService();
        }
        private IEnumUtilityService CreateEnumUtilityService()
        {
            return new EnumUtilityService();
        }
        private void SetupEnumUtilityService()
        {
            _enumUtilityService = CreateEnumUtilityService();
        }
        private ITypeUtilityService CreateTypeUtilityService()
        {
            return new TypeUtilityService();
        }
        private void SetupTypeUtilityService()
        {
            _typeUtilityService = CreateTypeUtilityService();
        }
        private IJsonUtilityService CreateJsonUtilityService()
        {
            return new JsonUtilityService(_typeUtilityService);
        }
        private void SetupJsonUtilityService()
        {
            _jsonUtilityService = CreateJsonUtilityService();
        }

        private GeminiToolRegistry CreateRegistry()
        {
            return new GeminiToolRegistry(_reflectionUtilityService);
        }
        private void SetupRegistry()
        {
            _geminiToolRegistry = CreateRegistry();
        }
        private GeminiToolConverter CreateToolConverter()
        {
            return new GeminiToolConverter(
                _jsonUtilityService,
                _enumUtilityService
            );
        }
        private void SetupToolConverter()
        {
            _geminiToolConverter = CreateToolConverter();
        }
        private GeminiToolDispatcher CreateDispatcher()
        {
            return new GeminiToolDispatcher(_geminiToolRegistry,_typeUtilityService);
        }
        private void SetupDispatcher()
        {
            _geminiToolDispatcher = CreateDispatcher();
        }
        // private GeminiToolExecutor CreateToolExecutor()
        // {
        //     return new GeminiToolExecutor(
        //         _geminiToolRegistry,
        //         _typeUtilityService
        //     );
        // }
        // private void SetupToolExecutor()
        // {
        //     _geminiToolExecutor = CreateToolExecutor();
        // }

        [Fact]
        public void CallOneMethod_ShouldBehaveAsExpected()
        {
            // Arrange
            // 這裡可以設置測試所需的環境和數據

            // Act
            // 執行你想要測試的功能

            var targetType = typeof(AnswersService);
            var getAnswer_MethodInfo = targetType.GetMethod("GetAnswer");
            _reflectionUtilityService.AddFastDelegate(getAnswer_MethodInfo!);

            var message = "Banana is a fruit";
            var category = "Warning";
            var description = "Gemini is an AI model, it can make mistakes";

            var jsonInput = $$"""
                {"answer":"{{message}}","category":"{{category}}","description":"{{description}}"}
            """;


            var jsonDoc = JsonDocument.Parse(jsonInput).RootElement;
            var argumentsDict = jsonDoc.EnumerateObject().ToDictionary(x => x.Name , x => x.Value);

            var answersService = new AnswersService();

            var getAnswer_Delegate = _reflectionUtilityService.FastDelegates [ 0 ];
            var getAnswer_Parameters = getAnswer_MethodInfo!.GetParameters();
            var getAnswer_Arguments = _reflectionUtilityService.BindArguments(getAnswer_Parameters , argumentsDict);

            var result_by_invoking_delegate = getAnswer_Delegate!(answersService , getAnswer_Arguments);

            var result_by_method_call = answersService.GetAnswer(message , category , description);

            _output.WriteLine("Result by invoking delegate: " + result_by_invoking_delegate);

            _output.WriteLine("Result by method call: " + result_by_method_call);

            // Assert
            // 驗證結果是否符合預期

            Assert.Equal(result_by_invoking_delegate , result_by_method_call);
        }

        [Fact]
        public void RegisterAllMethodsMarkedAsAttributeOfOneClass_ShouldRegisterSpecificMethod()
        {
            // Arrange
            // 這裡可以設置測試所需的環境和數據

            // Act
            // 執行你想要測試的功能

            var targetType = typeof(AnswersService);
            _geminiToolRegistry.Register(() => new AnswersService());

            bool isSuccess = _geminiToolRegistry.TryGetTool("GetAnswer" , out var metadata);

            _output.WriteLine($"isSuccess:{isSuccess}");
            _output.WriteLine($"metadata:{metadata}");
            _output.WriteLine($"metadata!.FastInvoke:{metadata!.FastInvoke}");
            _output.WriteLine($"metadata!.InstanceFactory:{metadata!.InstanceFactory}");
            _output.WriteLine($"metadata!.InstanceFactory!():{metadata!.InstanceFactory!()}");
           

            // Assert
            // 驗證結果是否符合預期
            Assert.True(isSuccess);
            Assert.NotNull(metadata);
        }

        [Fact]
        public void RegisterAllMethodsMarkedAsAttributeOfOneClass_ShouldHavingExactlyCount()
        {
            // Arrange
            // 這裡可以設置測試所需的環境和數據

            // Act
            // 執行你想要測試的功能

            var targetType = typeof(AnswersService);
            _geminiToolRegistry.Register(() => new AnswersService());

            var metadatas = _geminiToolRegistry.GetAllTools();

            _output.WriteLine($"metadatas.Count():{metadatas!.Count()}");

            // Assert
            // 驗證結果是否符合預期
            Assert.Equal(metadatas!.Count()!,1);
        }

        [Fact]
        public void RegisterAllMethodsMarkedAsAttributeOfOneAssembly_ShouldHavingExactlyCount()
        {
            // Arrange
            // 這裡可以設置測試所需的環境和數據

            // Act
            // 執行你想要測試的功能

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            _geminiToolRegistry.RegisterFromAssembly(executingAssembly);

            var metadatas = _geminiToolRegistry.GetAllTools();

            foreach(var metadata in metadatas)
            {
                _output.WriteLine($"metadata:{metadata}");
            }
            _output.WriteLine($"metadatas.Count():{metadatas!.Count()}");

            // Assert
            // 驗證結果是否符合預期
            Assert.Equal(metadatas!.Count()!,4);
        }

        [Fact]
        public void ConvertToolMetadataBaseToGeminiToolDeclaration_ShouldBehaveExpectedly()
        {
            // Arrange
            // 這裡可以設置測試所需的環境和數據

            // Act
            // 執行你想要測試的功能

            var targetType = typeof(AnswersService);
            _geminiToolRegistry.Register(() => new AnswersService());

            bool isSuccess = _geminiToolRegistry.TryGetTool("GetAnswer" , out var metadata);

            Assert.NotNull(metadata);

            var toolDeclaration =  _geminiToolConverter.ToToolDeclaration(metadata);

            // Assert
            // 驗證結果是否符合預期
            _output.WriteLine($"toolDeclaration!:{toolDeclaration!}");
            _output.WriteLine($"toolDeclaration!.Name:{toolDeclaration!.Name}");
            _output.WriteLine($"toolDeclaration!.Description:{toolDeclaration!.Description}");
            _output.WriteLine($"toolDeclaration!.Parameters:{toolDeclaration!.Parameters}");

            Assert.True(isSuccess);
            Assert.NotNull(metadata);
            Assert.NotNull(toolDeclaration);
            Assert.NotEmpty(toolDeclaration.Description);
            Assert.NotNull(toolDeclaration.Parameters);

        }

        [Fact]
        public async Task FastInvoke_ShouldBehaveExpectedly()
        {
            // Arrange
            // 這裡可以設置測試所需的環境和數據

            // Act
            // 執行你想要測試的功能

            var targetType = typeof(AnswersService);
            _geminiToolRegistry.Register(() => new AnswersService());

            // SetupToolExecutor();
            SetupDispatcher();

            bool isSuccess = _geminiToolRegistry.TryGetTool("GetAnswer" , out var metadata);

            var toolDeclaration = _geminiToolConverter.ToToolDeclaration(metadata);

            var result = await _geminiToolDispatcher.DispatchAsync(
                toolDeclaration!.Name, 
            new Dictionary<string , object>
            {
                { "answer" , "Banana is a fruit" },
                { "category" , "Warning" },
                { "description" , "Gemini is an AI model, it can make mistakes" }
            });

            var result_by_method_call = (new AnswersService()).GetAnswer("Banana is a fruit" , "Warning" , "Gemini is an AI model, it can make mistakes");

            // Assert
            // 驗證結果是否符合預期
            _output.WriteLine($"toolDeclaration:{toolDeclaration!}");
            _output.WriteLine($"toolDeclaration.Name:{toolDeclaration!.Name}");
            _output.WriteLine($"result (string type):{result}");
            _output.WriteLine($"result_by_method_call:{result_by_method_call}");

            Assert.True(isSuccess);
            Assert.NotNull(metadata);

            Assert.NotNull(toolDeclaration);
            Assert.Equal((string)result! , result_by_method_call);
        }
    }

}
