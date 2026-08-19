using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiCandidate
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public GeminiMessage Content { get; set; } = new();

        public GeminiCandidate Clone()
        {
            var clone = this?.MemberwiseClone() as GeminiCandidate ?? throw new InvalidCastException("Can't convert to GeminiCandidate while cloning.");
            return clone;
        }
        public GeminiCandidate? NullableClone()
        {
            var clone = this?.MemberwiseClone() as GeminiCandidate;
            return clone;
        }
    }
}
