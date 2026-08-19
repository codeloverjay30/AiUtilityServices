using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.AiBaseUtilityServices.Services
{
    public class StringFormmattingUtilityService: IStringFormmattingUtilityService
    {
        public string FormatWithMemory(
            string template,
            ReadOnlyMemory<char> input
        )
        {
            ReadOnlySpan<char> inputSpan = input.Span;

            // 計算總長度：模板長度 + 輸入長度
            int totalLength = template.Length + inputSpan.Length;

            return string.Create(totalLength , (template , input) , (chars , state) =>
            {
                // 寫入 "X"
                state.template.AsSpan().CopyTo(chars);
                // 在 X 之後寫入輸入內容
                state.input.Span.CopyTo(chars.Slice(state.template.Length));
            });
        }

        public ReadOnlyMemory<char> FormatWithMemoryAsReadOnlySpanOfChar(string template , ReadOnlyMemory<char> input)
        {
            return FormatWithMemory(template, input).AsMemory();
        }
    }
}
