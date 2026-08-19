using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Models
{
    /// <summary>
    /// A setting for a part used for Gemini AI model 
    /// </summary>
    public class GeminiSafetySetting
    {
        [System.Text.Json.Serialization.JsonPropertyName("category")]
        public string Category { get; set; } = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.SafetySetting.HARM_CATEGORY_HARASSMENT; // "HARM_CATEGORY_HARASSMENT"

        [System.Text.Json.Serialization.JsonPropertyName("threshold")]
        public string Threshold { get; set; } = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.SafetySetting.BLOCK_NONE; // "BLOCK_NONE"

        public GeminiSafetySetting DeepClone()
        {
            var clone = this?.MemberwiseClone() as GeminiSafetySetting ?? throw new InvalidCastException("Can't convert to GeminiSafetySetting while cloning");
            return clone; 
        }
    }
}
