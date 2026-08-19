using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiPart
    {
        /// <summary>
        /// For better performance due to zero allocation when split it (rather than `string`).
        /// </summary>

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonIgnore]
        public ReadOnlyMemory<char> RawText
        {
            get => string.IsNullOrEmpty(Text) ? ReadOnlyMemory<char>.Empty : Text.AsMemory();
            set => Text = value.Span.ToString();
        }
        [JsonPropertyName("inline_data")]
        public GeminiInlineData? InlineData { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("function_call")]
        public GeminiFunctionCall? FunctionCall { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("function_response")]
        public GeminiFunctionResponse? FunctionResponse { get; set; }

        public GeminiPart DeepClone()
        {
            return new GeminiPart
            {
                Text = this.Text ,
                InlineData = this.InlineData?.NullableDeepClone() ,
                FunctionCall = this.FunctionCall?.DeepClone() ,
                FunctionResponse = this.FunctionResponse?.NullableDeepClone(),
            };
        }
    }
}
