extern alias MimeTypeAlias;
extern alias TypeAlias;

using MimeTypes = MimeTypeAlias::CommonConstants.MimeTypes;
using TypeConstants = TypeAlias::CommonConstants.Types.TypeConstants;

using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiFunctionParameters
    {
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = TypeConstants.OBJECT; // "object"

        [System.Text.Json.Serialization.JsonPropertyName("properties")]
        public Dictionary<string , GeminiFunctionProperty> Properties { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("required")]
        public List<string> Required { get; set; } = new();

        public GeminiFunctionParameters DeepClone()
        {
            ArgumentNullException.ThrowIfNull(this);
            var clone = new GeminiFunctionParameters
            {
                Type = this.Type,
                Properties = this.Properties.ToDictionary(
                   entry => entry.Key,
                   entry => entry.Value.DeepClone()
                ),
                Required = this.Required.Select(t=> new String(t)).ToList(),
            };
            return clone;
        }
    }
}
