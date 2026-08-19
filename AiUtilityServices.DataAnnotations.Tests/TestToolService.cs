using AiUtility.GeminiKits.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtilityServices.DataAnnotations.Tests
{
    public class TestToolService
    {
        [GeminiTool(Description = "測試相加功能")]
        public int AddNumbers(int a , int b , string note = "default") => a + b;

        public void NotATool() { } // 不應該被偵測到
    }
}
