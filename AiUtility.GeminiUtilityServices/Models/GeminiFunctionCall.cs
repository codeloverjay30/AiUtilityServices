using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiFunctionCall
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("args")]
        public Dictionary<string , System.Text.Json.JsonElement> Args { get; set; } = new();

        public GeminiFunctionCall DeepClone()
        {
            ArgumentNullException.ThrowIfNull(this);
            var clone = new GeminiFunctionCall
            {
                Name = Name ,
                Args = this.Args?.ToDictionary(
                    entry => entry.Key ,
                    entry => entry.Value.Clone()
                ) ?? new()
            };

            return clone;
        }
    }
}
