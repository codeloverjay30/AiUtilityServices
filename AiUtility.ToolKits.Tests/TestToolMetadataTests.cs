using System.Reflection;
using System.ComponentModel.DataAnnotations;
using AiUtility.ToolKits.Tests;
using Xunit;
using System.ComponentModel;
using CustomDataAnnotations.Maintenance;

namespace AiUtility.ToolKits.UnitTests
{
    public class TestToolMetadataTests
    {

        [Fact]
        public void MethodAttributes_ShouldBeReadOnly()
        {
            // Arrange
            var methodInfo = typeof(Calculator).GetMethod(nameof(Calculator.Reset));
            var attributes = new List<Attribute> { new DisplayAttribute() };

            var metadata = new TestToolMetadata(
                "Reset" , methodInfo! , methodInfo!.GetParameters() ,
                (o , a) => null , null , attributes);

            // Assert
            // 驗證型別為 IReadOnlyList
            Assert.IsAssignableFrom<IReadOnlyList<Attribute>>(metadata.MethodAttributes);
        }

        [Fact]
        public void Constructor_ShouldPopulateMethodAttributes_WhenProvided()
        {
            // Arrange
            var methodInfo = typeof(Calculator).GetMethod(nameof(Calculator.Reset));
            var parameters = methodInfo!.GetParameters();
            var attributes = methodInfo.GetCustomAttributes<Attribute>().ToList();

            // Act
            var metadata = new TestToolMetadata(
                name: nameof(Calculator.Reset) ,
                mi: methodInfo ,
                p: parameters ,
                fi: (obj , args) => { ((Calculator)obj!).Reset(); return null; } ,
                fac: () => new Calculator() ,
                methodAttrs: attributes
            );

            // Assert
            Assert.Single(metadata.MethodAttributes);
            Assert.IsType<DisplayAttribute>(metadata.MethodAttributes [ 0 ]);
        }

        [Fact]
        public void MethodAttributes_ShouldCaptureTechnicalDebtWithEnum()
        {
            // Arrange: 測試 Division 方法
            var methodInfo = typeof(Calculator).GetMethod(nameof(Calculator.Division));
            var attributes = methodInfo!.GetCustomAttributes<Attribute>().ToList();

            // Act
            var metadata = new TestToolMetadata(
                nameof(Calculator.Division) , methodInfo , methodInfo.GetParameters() ,
                (o , a) => null , null , attributes);

            // Assert
            var debtAttr = metadata.MethodAttributes.OfType<TechnicalDebtAttribute>().FirstOrDefault();

            Assert.NotNull(debtAttr);
            // 驗證第一個引數 CategoryType 是否為 NamingIssue
            Assert.Equal(CategoryType.NamingIssue , debtAttr.Category);
        }

        [Fact]
        public void Registry_ShouldOnlyIdentifyMethodsWithAttributes()
        {
            // 這個測試模擬未來 Registry 掃描 Calculator 的結果
            // 根據您的需求：只有 Reset, Add, Division, Divide 有 Attribute
            var allMethods = typeof(Calculator).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var methodsWithAttributes = allMethods
                .Where(m => m.GetCustomAttributes<Attribute>(inherit: false).Any())
                .Select(m => m.Name)
                .ToList();

            // Assert
            Assert.Equal(5 , methodsWithAttributes.Count);
            Assert.Contains(nameof(Calculator.Reset) , methodsWithAttributes);
            Assert.Contains(nameof(Calculator.Add) , methodsWithAttributes);
            Assert.Contains(nameof(Calculator.Division) , methodsWithAttributes);

            // 驗證 Plus 方法不應該在裡面 (因為它沒有 Method Attribute)
            Assert.DoesNotContain(nameof(Calculator.Plus) , methodsWithAttributes);
        }

        // --- 輔助測試類別 ---
        private class Calculator
        {
            public int Number { get; set; }
            [Display(Name = "Reset")]
            public void Reset() { Number = 0; }

            [Display(Name = "Plus")]
            [AmbientValue(null)]
            public int Add([Range(1 , 100)]  int a, [Range(1 , 100)]  int  b) { return a + b; }
            public int Plus([Range(1 , 100)]  int a, [Range(1 , 100)]  int  b) { return a + b; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            /// <remarks>
            /// Use <seealso cref="Divide(int, int)"/>
            /// </remarks>
            [Obsolete("Use <seealso cref=\"Divide(int, int)\"/>")]
            [TechnicalDebt(CategoryType.NamingIssue, "<seealso cref=\"Divide(int, int)\"/>")]
            public int Division([Range(1 , 100)]  int a, [Range(1 , 100)]  int  b) { return a / b; }
            public int Divide([Range(1 , 100)]  int a, [Range(1 , 100)]  int  b) { return a / b; }
        }
    }
}
