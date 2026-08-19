using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Models;
using AiUtility.GeminiKits.Services;
using AiUtility.ToolKits.Abstractions;
using EnumUtilityServices;
using JsonUtilityServices;
using Moq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Xunit;

namespace AiUtility.GeminiKits.Tests
{
    public class GeminiToolConverterTests
    {
        private readonly Mock<IJsonUtilityService> _mockJsonService;
        private readonly Mock<IEnumUtilityService> _mockEnumService;
        private readonly GeminiToolConverter _converter;

        private const string GlobalDefaultDesc = "Custom Global Default";
        private const string GlobalDefaultParamDesc = "Custom Param Default";

        public GeminiToolConverterTests()
        {
            _mockJsonService = new Mock<IJsonUtilityService>();
            _mockEnumService = new Mock<IEnumUtilityService>();

            // 測試時可以注入自定義的預設值來驗證引數是否生效
            _converter = new GeminiToolConverter(
                _mockJsonService.Object ,
                _mockEnumService.Object
            );
        }

        [Fact]
        public void ToToolDeclaration_ShouldUseGeminiToolAttributeDescription_WhenPresent()
        {
            // Arrange
            var methodInfo = typeof(TestToolbox).GetMethod(nameof(TestToolbox.MethodWithGeminiAttribute));
            var metadata = CreateMetadata(methodInfo!);

            // Act
            var result = _converter.ToToolDeclaration(metadata);

            // Assert
            Assert.Equal("Gemini specific description" , result.Description);
        }

        [Fact]
        public void ToToolDeclaration_ShouldUseDefaultDescription_WhenNoAttributePresent()
        {
            // Arrange: 使用完全沒有 Attribute 的方法
            var methodInfo = typeof(TestToolbox).GetMethod(nameof(TestToolbox.MethodWithoutAttribute));
            var metadata = CreateMetadata(methodInfo!);

            // Act
            var result = _converter.ToToolDeclaration(metadata);

            // Assert: 驗證是否使用了在 GeminiToolConverter 建構子中定義或常數定義的預設值
            // 這裡假設您的 GeminiToolConverter 傳給基底的是 AiToolConstants.DefaultDescription
            Assert.False(string.IsNullOrEmpty(result.Description));
        }

        [Fact]
        public void ToToolDeclaration_ShouldIncludeEnumValues_WhenParameterIsEnum()
        {
            // Arrange
            var methodInfo = typeof(TestToolbox).GetMethod(nameof(TestToolbox.MethodWithEnum));
            var metadata = CreateMetadata(methodInfo!);

            _mockEnumService.Setup(s => s.GetEnumNames(
                It.IsAny<Type>()
            )) // 任何型別都回傳
               .Returns(new [ ] { "Unspecified" , "Utc" , "Local" });

            _mockJsonService.Setup(s => s.GetJsonType(
                It.IsAny<Type>()
            ))
                .Returns(CommonConstants.Types.TypeConstants.STRING); // "string"

            var customConverter = CreateToolConverter(); // 使用專門為測試預設值的Helper method

            // Act
            GeminiToolDeclaration result = customConverter.ToToolDeclaration(metadata);
            var properties = result.Parameters.Properties;
            var kindParam = properties [ "kind" ];

            // Assert
            Assert.Equal("string" , kindParam.Type);
            Assert.NotNull(kindParam.Enum);
            Assert.Contains("Utc" , kindParam.Enum);
            Assert.Equal(3 , kindParam.Enum.Count);
        }

        [Fact]
        public void ToToolDeclaration_ShouldUseDefaultParameterDescription_WhenParamHasNoDescription()
        {
            // Arrange
            var methodInfo = typeof(TestToolbox).GetMethod(nameof(TestToolbox.MethodWithNoParamDesc));
            var metadata = CreateMetadata(methodInfo!);
            _mockJsonService.Setup(s => s.GetJsonType(It.IsAny<Type>())).Returns("string");

            // Act
            var customConverter = CreateToolConverter(); // 使用專門為測試預設值的Helper method
            var result = customConverter.ToToolDeclaration(metadata);
            var properties = result.Parameters.Properties;
            GeminiParameterProperty? inputParam = null;
            properties.TryGetValue("input" , out inputParam);

            // Assert: 驗證參數是否拿到了預設描述 (而不是空字串)
            Assert.False(string.IsNullOrEmpty(inputParam?.Description));
        }

        // --- Helper Methods ---

        private GeminiToolConverter CreateToolConverter()
        {
            // 建立一個專門測試預設值的實例
            return new GeminiToolConverter(
                _mockJsonService.Object ,
                _mockEnumService.Object ,
                "DefaultToolDesc" , // 明確傳入預設參數描述
                "ExpectedParamDesc" // 明確傳入預設參數描述
            );
        }

        private GeminiToolMetadata CreateMetadata(MethodInfo mi)
        {
            return new GeminiToolMetadata(
                mi.Name ,
                mi ,
                mi.GetParameters() ,
                (o , a) => null ,
                null ,
                mi.GetCustomAttributes<Attribute>()
            );
        }

        // --- Test Data Toolbox ---
        private class TestToolbox
        {
            [GeminiTool(Description = "Gemini specific description")]
            public void MethodWithGeminiAttribute() { }

            [Description("Standard description")]
            public void MethodWithStandardAttribute() { }

            public void MethodWithoutAttribute() { }

            public void MethodWithEnum(DateTimeKind kind) { }

            public void MethodWithNoParamDesc(string input) { }
        }
    }
}
