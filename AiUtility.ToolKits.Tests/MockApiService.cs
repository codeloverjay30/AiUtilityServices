using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.ToolKits.Tests
{
    public class MockApiService
    {
        [TestTool]
        public string Greet(string name) => $"Hello, {name}";

        // 沒有標記的方法不應被註冊
        public void IgnoreMe() { }
    }
}
