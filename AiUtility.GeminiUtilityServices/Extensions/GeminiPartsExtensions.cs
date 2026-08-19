using AiUtility.GeminiUtilityServices.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Extensions
{
    public static class GeminiPartsExtensions
    {
        public static List<GeminiPart> ToGeminiParts(this List<object> parts) => parts.ConvertAll(p => p.ToGeminiPart());
    }
}
