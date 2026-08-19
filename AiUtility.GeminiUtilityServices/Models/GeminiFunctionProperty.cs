extern alias MimeTypeAlias;
extern alias TypeAlias;

using MimeTypes = MimeTypeAlias::CommonConstants.MimeTypes;
using TypeConstants = TypeAlias::CommonConstants.Types.TypeConstants;

using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiFunctionProperty
    {
        /// <summary>
        /// type of `function` that will be executed
        /// </summary>
        /// <remarks>
        /// Must be one of string, number, integer, boolean
        /// </remarks>
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = TypeConstants.STRING; // "string"

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        public GeminiFunctionProperty DeepClone()
        {
            var clone = this?.MemberwiseClone() as GeminiFunctionProperty ?? throw new InvalidCastException("Can't convert to GeminiFunctionProperty while cloning.");
            return clone;
        }
    }
}
