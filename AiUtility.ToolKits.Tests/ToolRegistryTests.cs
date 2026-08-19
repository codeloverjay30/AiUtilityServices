using Xunit;
using AiUtility.ToolKits.Registry;
using AiUtility.ToolKits.Tests; // 確保引用了 TestToolMetadata 所在的命名空間
using System.Linq;
using System.Reflection;

namespace AiUtility.ToolKits.Tests
{
    public class ToolRegistryTests
    {
        [Fact]
        public void Register_ShouldDiscoverMethods_WhenAttributeIsPresent()
        {
            // Arrange
            // 修正點：metadataFactory 的 Lambda 增加第三個參數 'attrs'
            // 並將其傳遞給 TestToolMetadata 的建構子
            var registry = new ToolRegistry<TestToolMetadata , TestToolAttribute>((mi , resolver) =>
            {
                // 模擬抓取該方法的所有 Attributes
                var attrs = mi.GetCustomAttributes<Attribute>();

                return new TestToolMetadata(
                    mi.Name ,
                    mi ,
                    mi.GetParameters() ,
                    (inst , args) => null ,
                    null ,
                    attrs // 傳遞給修正後的建構子參數
                );
            });

            // Act
            registry.Register<MockApiService>(() => new MockApiService());

            // Assert
            bool found = registry.TryGetTool("Greet" , out var metadata);

            Assert.True(found);
            Assert.NotNull(metadata);
            Assert.Equal("Greet" , metadata.FunctionName);
            Assert.Single(metadata.Parameters);

            // 額外驗證：確認 Attributes 是否有被正確存入 MethodAttributes
            Assert.Contains(metadata.MethodAttributes , a => a is TestToolAttribute);
        }
    }
}
