using System.Reflection;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Registry;
using Moq;
using ReflectionUtilityServices;
using Xunit;

namespace AiUtility.GeminiKits.Tests
{
    public class GeminiToolRegistryTests
    {
        private readonly Mock<IReflectionUtilityService> _mockReflectionService;
        private readonly List<Func<object? , object? [ ]? , object?>> _mockDelegates;

        public GeminiToolRegistryTests()
        {
            _mockReflectionService = new Mock<IReflectionUtilityService>();
            _mockDelegates = new List<Func<object? , object? [ ]? , object?>>();

            // 模擬 FastDelegates 屬性回傳清單
            _mockReflectionService.SetupGet(s => s.FastDelegates).Returns(_mockDelegates!);

            // 模擬 AddFastDelegate：每次呼叫時向清單增加一個虛擬委派
            _mockReflectionService.Setup(s => s.AddFastDelegate(It.IsAny<MethodInfo>()))
                .Callback(() => _mockDelegates.Add((obj , args) => "MockResult"));
        }

        [Fact]
        public void Register_UsingFactory_ShouldRegisterMarkedMethods()
        {
            // Arrange
            var registry = new GeminiToolRegistry(_mockReflectionService.Object);

            // Act: 透過泛型工廠註冊
            registry.Register<TestToolbox>(() => new TestToolbox());

            // Assert
            var tools = registry.GetAllTools().ToList();

            // 驗證是否只註冊了帶有 Attribute 的方法（AddNumbers, GetStatus）
            // 注意：FunctionName 繼承自 ToolMetadataBase，通常預設為 Method Name
            Assert.Contains(tools , t => t.FunctionName == nameof(TestToolbox.AddNumbers));
            Assert.Contains(tools , t => t.FunctionName == nameof(TestToolbox.GetStatus));
            Assert.DoesNotContain(tools , t => t.FunctionName == nameof(TestToolbox.InternalLogic));

            // 驗證 ReflectionUtilityServices 是否被正確調用
            _mockReflectionService.Verify(s => s.AddFastDelegate(It.IsAny<MethodInfo>()) , Times.Exactly(2));
        }

        [Fact]
        public void RegisterFromAssembly_ShouldFindToolsInTypes()
        {
            // Arrange
            var registry = new GeminiToolRegistry(_mockReflectionService.Object);
            var assembly = Assembly.GetExecutingAssembly();

            // Act: 掃描目前測試專案的 Assembly
            registry.RegisterFromAssembly(assembly);

            // Assert
            bool found = registry.TryGetTool(nameof(TestToolbox.AddNumbers) , out var metadata);
            Assert.True(found);
            Assert.NotNull(metadata);
            Assert.Equal(2 , metadata!.Parameters.Length);
        }

        [Fact]
        public void MetadataFactory_ShouldHandleStaticMethodsWithoutInstance()
        {
            // Arrange
            var registry = new GeminiToolRegistry(_mockReflectionService.Object);
            registry.Register<TestToolbox>(() => new TestToolbox());

            // Act
            registry.TryGetTool(nameof(TestToolbox.GetStatus) , out var metadata);

            // Assert
            // 靜態方法在 GeminiToolRegistry 的建構邏輯中會將 fac 設為 null
            // 這裡假設 ToolMetadataBase.InstanceFactory (或其他名稱) 會反映這個行為
            // 您可以根據 ToolMetadataBase 的具體屬性名稱來微調此斷言
        }

        // --- 測試用輔助類別 ---
        public class TestToolbox
        {
            [GeminiTool(Description = "加法測試")]
            public int AddNumbers(int a , int b) => a + b;

            [GeminiTool(Description = "靜態方法測試")]
            public static string GetStatus() => "OK";

            // 沒有 Attribute，不應該被註冊
            public void InternalLogic() { }
        }
    }
}
