using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.ToolKits.Models
{
    public abstract class AiParametersBase
    {
        public string Type { get; set; } = "object";

        // 使用基底屬性類別
        public Dictionary<string , AiParameterPropertyBase> Properties { get; set; } = new();

        public List<string> Required { get; set; } = new();
    }
}
