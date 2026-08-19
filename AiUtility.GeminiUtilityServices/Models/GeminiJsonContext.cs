using AiUtility.Common.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AiUtility.GeminiUtilityServices.Models
{
    [JsonSerializable(typeof(GeminiGenerateRequest))]
    [JsonSerializable(typeof(GeminiResponse))]
    [JsonSerializable(typeof(GeminiPart))]
    [JsonSerializable(typeof(Dictionary<string , object>))] // 若需要處理動態物件，需加入此行
    public partial class GeminiJsonContext : JsonSerializerContext
    {
        
    }
}
