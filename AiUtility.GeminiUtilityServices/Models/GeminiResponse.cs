using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("candidates")]
        public List<GeminiCandidate> Candidates { get; set; } = new();

        /// <summary>
        /// 取得第一條候選回覆中的第一個 Part。
        /// 使用 OfType<GeminiPart> 確保在 Parts 為 List<object> 時能正確識別。
        /// </summary>
        [JsonIgnore]
        public GeminiPart? FirstPart => Candidates?.FirstOrDefault()?.Content?.Parts?
                                        .OfType<GeminiPart>()
                                        .FirstOrDefault();

        /// <summary>
        /// 取得 AI 回傳的純文字內容 (常用於摘要或對話)
        /// </summary>
        [JsonIgnore]
        public string Text => FirstPart?.Text ?? string.Empty;
        [JsonIgnore]
        public ReadOnlyMemory<char> RawText => FirstPart?.RawText ?? ReadOnlyMemory<char>.Empty;

        // 快捷屬性：取得工具呼叫資訊
        public GeminiFunctionCall? FunctionCall => FirstPart?.FunctionCall;


        /// <summary>
        /// The number of token used from API response.
        /// </summary>
        /// <remarks>
        /// </remarks>
        [System.Text.Json.Serialization.JsonPropertyName("usageMetadata")]
        public GeminiUsageMetadata? UsageMetadata { get; set; }

        public GeminiResponse DeepClone()
        {
            ArgumentNullException.ThrowIfNull(this);
            var clone = new GeminiResponse
            {
                Candidates = this.Candidates.Select(t=>t.Clone()).ToList(),
            };
            return clone;
        }
    }
}
