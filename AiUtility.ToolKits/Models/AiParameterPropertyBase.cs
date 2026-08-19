using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.ToolKits.Models
{
    public abstract class AiParameterPropertyBase
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string>? Enum { get; set; }
    }
}
