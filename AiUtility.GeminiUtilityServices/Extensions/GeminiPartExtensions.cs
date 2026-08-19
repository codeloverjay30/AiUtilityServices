using AiUtility.GeminiUtilityServices.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Extensions
{
    public static class GeminiPartExtensions
    {
        /// <summary>
        /// Utility method to convert various types of input into a `GeminiPart` instance.
        /// It supports:
        ///
        /// + `GeminiPart`: If the input is already a `GeminiPart`, it returns it directly.
        /// + `string`: If the input is a string, it creates a new `GeminiPart` with the `Text` property set to that string.
        /// + `GeminiInlineData`: If the input is a `GeminiInlineData`, it creates a new `GeminiPart` with the `InlineData` property set to that data.
        /// + `GeminiFunctionCall`: If the input is a `GeminiFunctionCall`, it creates a new `GeminiPart` with the `FunctionCall` property set to that function call.
        /// + `GeminiFunctionResponse`: If the input is a `GeminiFunctionResponse`, it creates a new `GeminiPart` with the `FunctionResponse` property set to that function response.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">The exception throws when the <paramref name="part"/> is not one of the above type.</exception>
        public static GeminiPart ToGeminiPart(this object part)
        {
            switch(part)
            {
                case GeminiPart gp:
                    return gp;
                case string text:
                    return new GeminiPart { RawText = text.AsMemory() };
                case ReadOnlyMemory<char> text:
                    return new GeminiPart { RawText = text };
                case GeminiInlineData inlineData:
                    return new GeminiPart { InlineData = inlineData };
                case GeminiFunctionCall functionCall:
                    return new GeminiPart { FunctionCall = functionCall };
                case GeminiFunctionResponse functionResponse:
                    return new GeminiPart { FunctionResponse = functionResponse };
                default:
                    var geminiPart = GeminiFastMapper.ToGeminiPart(part) ?? throw new ArgumentException($"Can't convert {part} to GeminiPart",nameof(part));
                    return geminiPart;
            }
        }
    }
}
