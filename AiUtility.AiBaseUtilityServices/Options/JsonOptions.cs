using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace AiUtility.Common.Options
{
    public static class JsonOptions
    {
        public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            // + 如果 JSON 裡面寫了 C# 類別沒定義的欄位，直接拋出 Exception 報錯
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow ,

            // + 開啟此項以序列化 public fields
            IncludeFields = true, 

            // + 允許反序列化時，將 JSON 中的字串讀取為 C# 的數字類型 (如:int) and
            // + 允許在序列化時，處理特殊浮點數常數 (如:Nan)，轉為 JSON 字串輸出 and
            // + 在序列化時，將 C# 的數字強制轉為 JSON 字串輸出。
            NumberHandling = JsonNumberHandling.AllowReadingFromString |
                             JsonNumberHandling.AllowNamedFloatingPointLiterals |
                             JsonNumberHandling.WriteAsString,

            // + 設定編碼器允許中文範圍不被轉義
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),

            // + 反序列化將不區分大小寫
            PropertyNameCaseInsensitive = true ,

            // + 輸出的Json會包含換行和縮排
            WriteIndented = true ,

            // + 允許註解，這對手寫組態檔的人非常友善，允許在 JSON 裡寫 // 或 /* */
            ReadCommentHandling = JsonCommentHandling.Skip ,

            // + 允許最後一個屬性後面有逗號
            AllowTrailingCommas = true , 
        };
    }
}
