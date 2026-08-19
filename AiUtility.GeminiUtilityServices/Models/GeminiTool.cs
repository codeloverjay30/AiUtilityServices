using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiTool
    {
        [System.Text.Json.Serialization.JsonPropertyName("function_declarations")]
        public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();

        public GeminiTool DeepClone()
        {
            ArgumentNullException.ThrowIfNull(this);
            var clone = new GeminiTool
            {
                FunctionDeclarations = this.FunctionDeclarations.Select(x => x.DeepClone()).ToList(),
            };

            return clone;
        }
    }
}
