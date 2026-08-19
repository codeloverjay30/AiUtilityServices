using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.DataAnnotations
{
    [AttributeUsage(AttributeTargets.All)]
    public class GeminiToolAttribute: Attribute
    {
        public string Description { get; set; }
    }
}
