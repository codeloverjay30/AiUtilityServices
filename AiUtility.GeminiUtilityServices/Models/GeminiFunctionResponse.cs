using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiFunctionResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public ReadOnlyMemory<char> RawName
        {
            get { return Name.AsMemory(); }
            set { Name = value.ToString(); }
        }

        /// <summary>
        /// the response of AI API call. 
        /// </summary>

        [System.Text.Json.Serialization.JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonIgnore]
        public ReadOnlyMemory<char> RawResponse
        {
            get {  return Response.AsMemory();  }
            set { Response = value.ToString(); }
        }

        public GeminiFunctionResponse DeepClone()
        {
            var clone = this?.MemberwiseClone() as GeminiFunctionResponse ?? throw new InvalidCastException("Can't convert to GeminiFunctionResponse while cloning.");
            return clone;
        }

        public GeminiFunctionResponse? NullableDeepClone()
        {
            var clone = this.MemberwiseClone() as GeminiFunctionResponse;
            return clone;
        }
    }
}
