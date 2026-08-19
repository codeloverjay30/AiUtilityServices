using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiUsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }

        public GeminiUsageMetadata DeepCopy()
        {
            var clone = this?.MemberwiseClone() as GeminiUsageMetadata ?? throw new InvalidCastException("Can't convert to GeminiUsageMetadata while clong.");
            return clone;
        }
    }
}
