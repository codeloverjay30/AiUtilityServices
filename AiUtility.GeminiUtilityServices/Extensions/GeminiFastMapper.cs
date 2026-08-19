using AiUtility.GeminiUtilityServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Extensions
{

    public static class GeminiFastMapper
    {
        // 建立一個靜態實例，並帶入你想要的 options
        private static readonly AiUtility.GeminiUtilityServices.Models.GeminiJsonContext _context =
            new AiUtility.GeminiUtilityServices.Models.GeminiJsonContext(AiUtility.Common.Options.JsonOptions.DefaultOptions);

        public static GeminiPart? ToGeminiPart(object obj)
        {
            // 使用你自定義的 context
            var json = JsonSerializer.Serialize(obj , obj.GetType() , _context);
            return (GeminiPart?)JsonSerializer.Deserialize(json , _context.GeminiPart);
        }
    }
}
