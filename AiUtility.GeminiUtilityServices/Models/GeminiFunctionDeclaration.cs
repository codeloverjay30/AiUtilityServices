using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiFunctionDeclaration
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("parameters")]
        public GeminiFunctionParameters Parameters { get; set; } = new();

        public GeminiFunctionDeclaration DeepClone()
        {
            ArgumentNullException.ThrowIfNull(this);
            var clone = new GeminiFunctionDeclaration
            {
                Name = this.Name,
                Description = this.Description,
                Parameters = this.Parameters.DeepClone(),
            };

            return clone;
        }
    }
}
