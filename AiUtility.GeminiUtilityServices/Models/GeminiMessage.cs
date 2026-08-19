using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiMessage
    {
        /// <summary>
        /// The role of the message sender, which can be either "user" or "model". This field is used to indicate who is sending the message in a conversation.
        /// </summary>
        /// <remarks>
        /// Must be one of
        /// + "user"
        /// + "model"
        /// </remarks>
        [System.Text.Json.Serialization.JsonPropertyName("role")]
        public string Role { get; set; } = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER; // "user"

        [System.Text.Json.Serialization.JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();

        public GeminiMessage Clone()
        {
            var clone = this?.MemberwiseClone() as GeminiMessage ?? throw new InvalidCastException("Can't convert it to GeminiMessage type while cloning.");
            if(this.Parts != null)
            {
                // 優化：預設 Capacity 避免 List 內部頻繁 Resize
                var newParts = new List<GeminiPart>(this.Parts.Count);
                foreach(var part in this.Parts)
                {
                    newParts.Add(part.DeepClone());
                }
                clone.Parts = newParts;
            }
            else
            {
                clone.Parts = new List<GeminiPart>();
            }
            return clone;
        }
        public GeminiMessage? NullableClone()
        {
            var clone = this?.MemberwiseClone() as GeminiMessage;
            if(clone==null)
            {
                clone = new GeminiMessage
                {
                    Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER, // "user"
                };
            }
            if(this.Parts != null)
            {
                // 優化：預設 Capacity 避免 List 內部頻繁 Resize
                var newParts = new List<GeminiPart>(this.Parts.Count);
                foreach(var part in this.Parts)
                {
                    newParts.Add(part.DeepClone());
                }
                clone.Parts = newParts;
            }
            else
            {
                clone.Parts = new List<GeminiPart>();
            }
            return clone;
        }
    }
}
