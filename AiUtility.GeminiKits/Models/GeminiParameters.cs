using AiUtility.ToolKits.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiKits.Models
{
    public class GeminiParameters : AiParametersBase
    {
        // 不要實例化新字典 (= new())，而是去存取基底的字典並轉型
        public new Dictionary<string , GeminiParameterProperty> Properties
        {
            get => base.Properties.ToDictionary(k => k.Key , v => (GeminiParameterProperty)v.Value);
            set => base.Properties = value.ToDictionary(k => k.Key , v => (AiParameterPropertyBase)v.Value);
        }
    }
}
