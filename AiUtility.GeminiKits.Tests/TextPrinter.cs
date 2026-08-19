using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiKits.Tests
{
    public class TextPrinter
    {
        private static readonly Dictionary<Color , string> ColorCodes = new()
        {
            { Color.Red , "\u001b[31m" },
            { Color.Green , "\u001b[32m" },
            { Color.Yellow , "\u001b[34m" },
        };
        public string Echo(string message,Color color)
        {
            var colorCode = ColorCodes[color];
            return $"{message} with color code {colorCode}";
        }
    }
}
