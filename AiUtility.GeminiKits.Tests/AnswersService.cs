using AiUtility.GeminiKits.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiKits.Tests
{
    public class AnswersService(string aiModelName = "Gemini")
    {
        [GeminiTool(Category = "Warning", Description = "AI can make mistake")]
        public string GetAnswer(string answer,string category,string description)
        {
            return $"[{aiModelName}] answers {answer} with {category}: {description}";
        }
    }
}
