using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.AiBaseUtilityServices.Services
{
    public interface IStringFormmattingUtilityService
    {
        string FormatWithMemory(
            string template ,
            ReadOnlyMemory<char> input
        );
        ReadOnlyMemory<char> FormatWithMemoryAsReadOnlySpanOfChar(
            string template ,
            ReadOnlyMemory<char> input
        );
    }
}
