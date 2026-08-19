using AiUtility.ToolKits.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiKits.Models
{
    public class GeminiToolDeclaration : AiToolDeclarationBase
    {
        // 透過屬性隱藏 (Shadowing) 或轉型提供強型別
        public new GeminiParameters Parameters
        {
            get => (GeminiParameters)base.Parameters;
            set => base.Parameters = value;
        }
    }
}
