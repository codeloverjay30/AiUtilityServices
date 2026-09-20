using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiFunctionCall
    {
        /// <summary>
        /// Gets or sets the identifier assigned to the function call.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("args")]
        public Dictionary<string , System.Text.Json.JsonElement> Args { get; set; } = new();

        public GeminiFunctionCall DeepClone()
        {
            ArgumentNullException.ThrowIfNull(this);
            var clone = new GeminiFunctionCall
            {
                Id = Id,
                Name = Name,
                Args = this.Args?.ToDictionary(
                    entry => entry.Key ,
                    entry => entry.Value.Clone()
                ) ?? new()
            };

            return clone;
        }
    }
}
