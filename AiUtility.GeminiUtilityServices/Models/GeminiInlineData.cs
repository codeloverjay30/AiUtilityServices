extern alias MimeTypeAlias;
extern alias TypeAlias;

using MimeTypes = MimeTypeAlias::CommonConstants.MimeTypes;
using TypeConstants = TypeAlias::CommonConstants.Types.TypeConstants;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiInlineData
    {
        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = MimeTypes.MimeTypeConstants.IMAGE_PNG; // image/png

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Helper getter-setter property to process the data from <see cref="GeminiInlineData.Data"/>.
        /// </summary>
        [JsonIgnore]
        public ReadOnlyMemory<byte> RawData
        {
            get => string.IsNullOrEmpty(Data) ? ReadOnlyMemory<byte>.Empty : Convert.FromBase64String(Data);
            set => Data = Convert.ToBase64String(value.Span);
        }

        public GeminiInlineData DeepClone()
        {
            var clone = this?.MemberwiseClone() as GeminiInlineData ?? throw new InvalidCastException("Can't convert to GeminiInlineData while cloning.");
            return clone;
        }
        public GeminiInlineData? NullableDeepClone()
        {
            var clone = this.MemberwiseClone() as GeminiInlineData;
            return clone;
        }
    }
}
