extern alias MimeTypeAlias;
extern alias TypeAlias;

using MimeTypes = MimeTypeAlias::CommonConstants.MimeTypes;
using TypeConstants = TypeAlias::CommonConstants.Types.TypeConstants;

using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.AiBaseUtilityServices.Services;
using AiUtility.GeminiUtilityServices.Extensions;
using AiUtility.GeminiUtilityServices.Services;
using CustomDataAnnotations.Maintenance;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace AiUtility.GeminiUtilityServices.Models
{
    public class GeminiGenerateRequest(IStringFormmattingUtilityService stringFormmattingUtilityService = null)
    {
        private static readonly IStringFormmattingUtilityService _defaultStringFormmattingUtilityService = new StringFormmattingUtilityService();
        private readonly IStringFormmattingUtilityService _stringFormmattingUtilityService = stringFormmattingUtilityService ?? _defaultStringFormmattingUtilityService;
        public string Prompt { get; set; } = string.Empty;
        [JsonIgnore]
        public ReadOnlyMemory<char> RawPrompt
        {
            get => string.IsNullOrEmpty(Prompt) ? ReadOnlyMemory<char>.Empty : Prompt.AsMemory();
            set => Prompt = value.Span.ToString();
        }
        public List<GeminiMessage> Contents { get; set; } = new();
        public string ResponseMimeType { get; set; } = MimeTypes.MimeTypeConstants.APPLICATION_JSON; // application/json
        public string? SystemInstruction { get; set; }

        [Range(double.Epsilon, AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionSettings.AVAILABLE_MAX_TEMPERATURE + double.Epsilon , ErrorMessage = AiUtility.AiBaseUtilityServices.Consts.Constants.Constraints.ValueConstraints.TEMPERATURE_MUST_BETWEEN_ZERO_AND_TWO)]
        public double Temperature { get; set; } = AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionSettings.DEFAULT_TEMPERATURE;

        public int MaxOutputTokens { get; set; } = AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionSettings.DEFAULT_MAX_TOKENS;

        public object? ResponseSchema { get; set; }

        /// <summary>
        /// A list of safety settings used for Gemini AI model
        /// </summary>
        public List<GeminiSafetySetting> SafetySettings { get; set; } = new();

        /// <summary>
        /// Contains tools used for Gemini AI model
        /// </summary>

        [JsonPropertyName("tools")]
        public List<GeminiToolDeclarationWrapper> Tools { get; set; } = new();

        // 定義 Wrapper (Gemini 格式要求：[{ "function_declarations": [...] }])
        public class GeminiToolDeclarationWrapper
        {
            [JsonPropertyName("function_declarations")]
            public List<object> FunctionDeclarations { get; set; } = new();

            public GeminiToolDeclarationWrapper Clone()
            {
                var clone = (GeminiToolDeclarationWrapper)this.MemberwiseClone();
                clone.FunctionDeclarations = new List<object>(this.FunctionDeclarations);
                return clone;
            }
        }

        /// <summary>
        /// Utility method:
        /// Set the <see cref="ResponseSchema"/> given generator (<paramref name="schemaGenerator"/>)
        /// </summary>
        /// <typeparam name="T">Expected type that the schema after generating</typeparam>
        /// <param name="schemaGenerator">Generator</param>
        public void SetResponseSchema<T>(IGeminiSchemaGenerator schemaGenerator)
        {
            this.ResponseSchema = schemaGenerator.Generate<T>();
            this.ResponseMimeType = MimeTypes.MimeTypeConstants.APPLICATION_JSON; // application/json
        }

        /// <summary>
        /// Set the current <see cref="Prompt"/> to <paramref name="promopt"/>
        /// </summary>
        /// <param name="promopt">prompt</param>
        public void SetPrompt(
            string promopt
        )
        {
            this.Prompt = promopt;
        }
        public void SetPrompt(
            ReadOnlyMemory<char> promopt
        )
        {
            this.RawPrompt = promopt;
        }

        /// <summary>
        /// Utility method:
        /// Easily to add <paramref name="message"/> into <see cref="Contents"/>
        /// </summary>
        /// <param name="message"><see cref="GeminiMessage"/></param>
        public void AddMessage(
            GeminiMessage message
        )
        {
            this.Contents.Add(message);
        }

        /// <inheritdoc cref="AddMessage(GeminiMessage)"/>
        /// <remarks>
        /// The clone version of <seealso cref="AddMessage(GeminiMessage)"/> method
        /// </remarks>
        public GeminiGenerateRequest WithMessage(
            GeminiMessage message
        )
        {
            var clone = this.Clone();
            clone.Contents.Add(message);
            return clone;
        }

        /// <inheritdoc cref="AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)"/>
        /// For the extremely better performance, consider <seealso cref="AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)"/> method which has features: zero allocations.
        /// </remarks>
        [Obsolete("For the extremely better performance, consider <seealso cref=\"AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)\"/> method which has features: zero allocations.")]
        [TechnicalDebt(CategoryType.ExecutedTimePerformanceIssue, "AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)")]
        public void AddUserMessage(
            string text ,
            string base64Image,
            string mimeType = MimeTypes.MimeTypeConstants.IMAGE_PNG // "image/png"
        )
        {
            var parts = new List<GeminiPart>();

            // 直接實例化明確型別 GeminiPart，不再使用匿名物件
            if(!string.IsNullOrEmpty(text))
            {
                parts.Add(new GeminiPart { Text = text });
            }

            if(!string.IsNullOrEmpty(base64Image) && !string.IsNullOrEmpty(mimeType))
            {
                parts.Add(new GeminiPart
                {
                    InlineData = new GeminiInlineData
                    {
                        MimeType = mimeType ,
                        Data = base64Image
                    }
                });
            }

            if(parts.Count > 0)
            {
                this.AddMessage(
                    new GeminiMessage()
                    {
                        Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER , // "user"
                        Parts = parts
                    }
                );
            }
        }

        /// <inheritdoc cref="AddUserMessage(string, string, string)"/>
        /// <param name="imageBytes">image represented by byte array</param>
        [Obsolete("For the extremely better performance, consider <seealso cref=\"AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)\"/> method which has features: zero allocations.")]
        [TechnicalDebt(CategoryType.ExecutedTimePerformanceIssue , "AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)")]
        public void AddUserMessage(
            string text ,
            byte [ ]? imageBytes = null ,
            string mimeType = MimeTypes.MimeTypeConstants.IMAGE_PNG // "image/png"
        )
        {
            var base64Image = imageBytes != null ? Convert.ToBase64String(imageBytes) : string.Empty;

            this.AddUserMessage(text , base64Image , mimeType);
        }

        /// <inheritdoc cref="AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)"/>
        public void AddUserMessage(
            ReadOnlyMemory<char> text
        )
        {
            AddUserMessage(text , ReadOnlyMemory<byte>.Empty);
        }

        /// <summary>
        /// Utility method:
        /// Easily add user <paramref name="text"/> and image <paramref name="base64Image"/> (if not set to be null) to <see cref="Contents"/>
        /// </summary>
        /// <param name="text">message</param>
        /// <param name="base64Image">one image representing by base-64. Added if it is neither set to be null nor empty</param>
        /// <param name="mimeType">the mime type for the image (<paramref name="base64Image"/>)</param>
        public void AddUserMessage(
            ReadOnlyMemory<char> text ,
            ReadOnlyMemory<byte> base64Image ,
            string mimeType = MimeTypes.MimeTypeConstants.IMAGE_PNG // "image/png"
        )
             {
            var parts = new List<GeminiPart>();

            // 直接實例化明確型別 GeminiPart，不再使用匿名物件
            if(!text.IsEmpty)
            {
                parts.Add(new GeminiPart { RawText = text });
            }

            if(!base64Image.IsEmpty && !string.IsNullOrEmpty(mimeType))
            {
                parts.Add(new GeminiPart
                {
                    InlineData = new GeminiInlineData
                    {
                        MimeType = mimeType ,
                        RawData = base64Image
                    }
                });
            }

            if(parts.Count > 0)
            {
                this.AddMessage(
                    new GeminiMessage()
                    {
                        Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER , // "user"
                        Parts = parts
                    }
                );
            }
        }

        /// <inheritdoc cref="WithUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)"/>
        /// <remarks>
        /// The cloned version of <seealso cref="AddUserMessage(string, string, string)"/> method
        /// </remarks>
        /// For the extremely better performance, consider <seealso cref="WithUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)"/> method which has features: zero allocations.
        /// </remarks>
        [Obsolete(" For the extremely better performance, consider <seealso cref=\"WithUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)\"/> method which has features: zero allocations.")]
        [TechnicalDebt(CategoryType.ExecutedTimePerformanceIssue , "WithUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)")]
        public GeminiGenerateRequest WithUserMessage(
            string text ,
            string base64Image,
            string mimeType = MimeTypes.MimeTypeConstants.IMAGE_PNG // "image/png"
        )
        {
            var clone = this.Clone();
            var parts = new List<GeminiPart>();

            // 直接實例化明確型別 GeminiPart，不再使用匿名物件
            if(!string.IsNullOrEmpty(text))
            {
                parts.Add(new GeminiPart { Text = text });
            }

            if(!string.IsNullOrEmpty(base64Image) && !string.IsNullOrEmpty(mimeType))
            {
                parts.Add(new GeminiPart
                {
                    InlineData = new GeminiInlineData
                    {
                        MimeType = mimeType ,
                        Data = base64Image
                    }
                });
            }

            if(parts.Count > 0)
            {
                clone = clone.WithMessage(
                    new GeminiMessage()
                    {
                        Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER , // "user"
                        Parts = parts
                    }
                );
            }

            return clone;
        }

        /// <inheritdoc cref="WithUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)"/>
        public GeminiGenerateRequest WithUserMessage(
            ReadOnlyMemory<char> text
        )
        {
            return WithUserMessage(text,ReadOnlyMemory<byte>.Empty);
        }

        /// <inheritdoc cref="AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)"/>
        /// <remarks>
        /// The cloned version of <seealso cref="AddUserMessage(ReadOnlyMemory{char}, ReadOnlyMemory{byte}, string)"/> method
        /// </remarks>
        public GeminiGenerateRequest WithUserMessage(
            ReadOnlyMemory<char> text ,
            ReadOnlyMemory<byte> base64Image ,
            string mimeType = MimeTypes.MimeTypeConstants.IMAGE_PNG // "image/png"
        )
        {
            var clone = this.Clone();
            var parts = new List<GeminiPart>();

            // 直接實例化明確型別 GeminiPart，不再使用匿名物件
            if(!text.IsEmpty)
            {
                parts.Add(new GeminiPart { RawText = text });
            }

            if(!base64Image.IsEmpty && !string.IsNullOrEmpty(mimeType))
            {
                parts.Add(new GeminiPart
                {
                    InlineData = new GeminiInlineData
                    {
                        MimeType = mimeType ,
                        RawData = base64Image
                    }
                });
            }

            if(parts.Count > 0)
            {
                clone = clone.WithMessage(
                    new GeminiMessage()
                    {
                        Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.USER , // "user"
                        Parts = parts
                    }
                );
            }

            return clone;
        }


        /// <inheritdoc cref="AddToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})"/>
        /// <remarks>
        /// For the extremely better performance, consider <seealso cref="AddToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})"/> method which has features: zero allocations.
        /// </remarks>
        [Obsolete("For the extremely better performance, consider <seealso cref=\"AddToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})\"/> method which has features: zero allocations.")]
        [TechnicalDebt(CategoryType.ExecutedTimePerformanceIssue , "AddToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})")]
        public void AddToolResponse(
            string functionName ,
            string aiResponse
        )
        {
            // 注意：Google 要求 Role 必須是 "function" (或是特定版本要求 user/model 配對)
            // 在 v1beta 中，通常是以 "user" 的身份回傳 functionResponse，或使用專屬角色
            var message = new GeminiMessage
            {
                Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.FunctionCall.FUNCTION, // function
                Parts = new List<GeminiPart>()
                {
                    new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            Name = functionName ,
                            Response = aiResponse // 注意：這裡簡化處理，實際使用中可能需要使用複雜的序列化
                        }
                    }
                }

            };
            this.Contents.Add(message);
        }

        /// <summary>
        /// Uility method:
        /// Add one <see cref="Contents"/> given <paramref name="functionName"/> and <paramref name="aiResponse"/>
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="aiResponse">response from AI model</param>
        public void AddToolResponse(
            ReadOnlyMemory<char> functionName ,
            ReadOnlyMemory<char> aiResponse
        )
        {
            // 注意：Google 要求 Role 必須是 "function" (或是特定版本要求 user/model 配對)
            // 在 v1beta 中，通常是以 "user" 的身份回傳 functionResponse，或使用專屬角色
            var message = new GeminiMessage
            {
                Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.FunctionCall.FUNCTION, // function
                Parts = new List<GeminiPart>()
                {
                    new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            RawName = functionName ,
                            RawResponse = aiResponse // 注意：這裡簡化處理，實際使用中可能需要使用複雜的序列化
                        }
                    }
                }

            };
            this.Contents.Add(message);
        }

        /// <inheritdoc cref="WithToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})"/>
        /// <remarks>
        /// For the extremely better performance, consider <seealso cref="WithToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})"/> method which has features: zero allocations.
        /// </remarks>
        [Obsolete("For the extremely better performance, consider <seealso cref=\"WithToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})\"/> method which has features: zero allocations.")]
        [TechnicalDebt(CategoryType.ExecutedTimePerformanceIssue , "WithToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})")]
        public GeminiGenerateRequest WithToolResponse(
            string functionName ,
            string aiResponse
        )
        {
            var clone = this.Clone(); // 取得深層複製的副本

            var message = new GeminiMessage
            {
                Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.FunctionCall.FUNCTION , // function
                Parts = new List<GeminiPart>
                {
                    new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            Name = functionName,
                            Response = aiResponse
                        }
                    }
                }
            };

            clone.Contents.Add(message);
            return clone;
        }

        /// <inheritdoc cref="AddToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})"/>
        /// <returns></returns>
        /// <remarks>
        /// the cloned version of <seealso cref="AddToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})"/> method
        /// see <seealso cref="AddToolResponse(ReadOnlyMemory{char}, ReadOnlyMemory{char})"/> method for more details.
        /// </remarks>
        public GeminiGenerateRequest WithToolResponse(
            ReadOnlyMemory<char> functionName ,
            ReadOnlyMemory<char> aiResponse
        )
        {
            var clone = this.Clone(); // 取得深層複製的副本

            var message = new GeminiMessage
            {
                Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.FunctionCall.FUNCTION , // function
                Parts = new List<GeminiPart>
                {
                    new GeminiPart
                    {
                        FunctionResponse = new GeminiFunctionResponse
                        {
                            RawName = functionName,
                            RawResponse = aiResponse
                        }
                    }
                }
            };

            clone.Contents.Add(message);
            return clone;
        }

        /// <summary>
        /// Purge all images except last Nth image to text or delete them to save the token.
        /// </summary>
        /// <param name="keepLastNImages">N in Last Nth image</param>
        public void PurgeOldMedia(
            int keepLastNImages = 2
        )
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keepLastNImages,nameof(keepLastNImages));

            int imageCount = 0;
            for(int i = Contents.Count - 1; i >= 0; i--)
            {
                var parts = Contents [ i ].Parts.OfType<GeminiPart>();
                foreach(var part in parts)
                {
                    if(part.InlineData != null)
                    {
                        imageCount++;
                        if(imageCount > keepLastNImages)
                        {
                            // Purge all images except last Nth image to text or delete them to save the token.
                            part.InlineData = null;
                            part.Text = AiUtility.AiBaseUtilityServices.Consts.Constants.AiTasks.Consolidations.PURGE_OLD_MEDIA_TO_SAVE_TOKEN_SPACE;
                        }
                    }
                }
            }
        }

        /// <inheritdoc cref="PurgeOldMedia(int)"/>
        /// <remarks>
        /// The cloned version of <seealso cref="PurgeOldMedia(int)"/> method.
        /// </remarks>
        public GeminiGenerateRequest WithPurgeOldMedia(
            int keepLastNImages = 2
        )
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keepLastNImages,nameof(keepLastNImages));

            var clone = this.Clone();
            int imageCount = 0;
            for(int i = clone.Contents.Count - 1; i >= 0; i--)
            {
                var parts = clone.Contents [ i ].Parts.OfType<GeminiPart>();
                foreach(var part in parts)
                {
                    if(part.InlineData != null)
                    {
                        imageCount++;
                        if(imageCount > keepLastNImages)
                        {
                            // Purge all images except last Nth image to text or delete them to save the token.
                            part.InlineData = null;
                            part.Text = AiUtility.AiBaseUtilityServices.Consts.Constants.AiTasks.Consolidations.PURGE_OLD_MEDIA_TO_SAVE_TOKEN_SPACE;
                        }
                    }
                }
            }

            return clone;
        }

        /// <summary>
        /// Compress the response except for the last n response where n is <see cref="AiExecutionSettings.LastTokenCountNeededToBeKept"/> of <paramref name="settings"/> and summarize it
        /// then concatenate them and the last 5 response
        /// to consolidate the memory.
        /// </summary>
        /// <param name="client"><seealso cref="IGeminiApiClient"/></param>
        /// <param name="currentTotalTokens">current used token</param>
        /// <param name="settings"><seealso cref="AiExecutionSettings"/></param>
        /// <returns></returns>
        public async Task ConsolidateMemoryAsync(
            IGeminiApiClient client,
            int currentTotalTokens ,
            AiExecutionSettings settings
        )
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(currentTotalTokens,nameof(currentTotalTokens));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Threshold,nameof(settings.Threshold));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.LastTokenCountNeededToBeKept,nameof(settings.LastTokenCountNeededToBeKept));
            // 如果對話太短則不處理
            if(currentTotalTokens < settings.Threshold)
            {
                return;
            }

            // 分離記憶
            var workingMemory = this.Contents.TakeLast(settings.LastTokenCountNeededToBeKept).ToList(); // 保留最後 n 輪
            var historicalData = this.Contents.SkipLast(settings.LastTokenCountNeededToBeKept).ToList(); // 準備壓縮的舊資料

            // 產生摘要 (Long-term Milestone)
            var summaryRequest = new GeminiGenerateRequest();
            summaryRequest.Contents.AddRange(historicalData);
            summaryRequest.AddUserMessage(AiUtility.AiBaseUtilityServices.Consts.Constants.AiTasks.Consolidations.SUMMARIZE_MILESTONE_TO_SAVE_SPACE.AsMemory());

            var response = await client.GenerateContentAsync(summaryRequest);
            var milestoneSummary = (response.Candidates [ 0 ].Content.Parts [ 0 ] as GeminiPart)?.RawText ?? ReadOnlyMemory<char>.Empty;

            // 重組內容
            this.Contents.Clear();

            // 插入長期記憶
            this.AddUserMessage(
                _stringFormmattingUtilityService.FormatWithMemoryAsReadOnlySpanOfChar(
                    AiUtility.AiBaseUtilityServices.Consts.Constants.AiTasks.Remembers.REVIEW_TASKS_AND_MILESTONE_FORMAT, milestoneSummary
            ));

            // 接回短期記憶
            this.Contents.AddRange(workingMemory);
        }
        public async Task<GeminiGenerateRequest> WithConsolidateMemoryAsync(
            IGeminiApiClient client,
            int currentTotalTokens ,
            AiExecutionSettings settings
        )
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(currentTotalTokens,nameof(currentTotalTokens));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.Threshold,nameof(settings.Threshold));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.LastTokenCountNeededToBeKept,nameof(settings.LastTokenCountNeededToBeKept));
            // 如果對話太短則不處理
            if(currentTotalTokens < settings.Threshold)
            {
                return this;
            }

            var clone = this.Clone();
            // 分離記憶
            var workingMemory = clone.Contents.TakeLast(settings.LastTokenCountNeededToBeKept).ToList(); // 保留最後 n 輪
            var historicalData = clone.Contents.SkipLast(settings.LastTokenCountNeededToBeKept).ToList(); // 準備壓縮的舊資料

            // 產生摘要 (Long-term Milestone)
            var summaryRequest = new GeminiGenerateRequest();
            summaryRequest.Contents.AddRange(historicalData);
            summaryRequest.AddUserMessage(AiUtility.AiBaseUtilityServices.Consts.Constants.AiTasks.Consolidations.SUMMARIZE_MILESTONE_TO_SAVE_SPACE.AsMemory());

            var response = await client.GenerateContentAsync(summaryRequest);
            var milestoneSummary = (response.Candidates [ 0 ].Content.Parts [ 0 ] as GeminiPart)?.RawText ?? ReadOnlyMemory<char>.Empty;

            // 重組內容
            clone.Contents.Clear();

            // 插入長期記憶
            clone = clone.WithUserMessage(
                _stringFormmattingUtilityService.FormatWithMemoryAsReadOnlySpanOfChar(
                    AiUtility.AiBaseUtilityServices.Consts.Constants.AiTasks.Remembers.REVIEW_TASKS_AND_MILESTONE_FORMAT,
                    milestoneSummary
            ));

            // 接回短期記憶
            clone.Contents.AddRange(workingMemory);
            return clone;
        }

        /// <summary>
        /// Convert the Model to Anomyous object for Gemini API. 
        /// </summary>
        /// <returns>Anomyous object</returns>
        public object ToGoogleApiRequest()
        {
            return new
            {
                contents = Contents ,
                system_instruction = SystemInstruction != null
                    ? new { parts = new [ ] { new { text = SystemInstruction } } }
                    : null ,
                tools = Tools.Count > 0 ? Tools : null ,
                safetySettings = SafetySettings.Count > 0 ? SafetySettings : null ,
                generationConfig = new
                {
                    temperature = Temperature ,
                    maxOutputTokens = MaxOutputTokens ,
                    response_mime_type = ResponseMimeType,
                    response_schema = ResponseSchema
                }
            };
        }
        public GeminiGenerateRequest Clone()
        {
            // 1. 利用 MemberwiseClone 快速複製所有屬性 (包含 Prompt, Config 等)
            var clone = (GeminiGenerateRequest)this.MemberwiseClone();

            // 2. 針對「會變動」的集合進行重新分配，避免 Race Condition
            // 雖然這裡有分配，但比起重新 new 整個複雜物件，開銷極小
            clone.Contents = this.Contents.Select(t => t.Clone()).ToList();
            clone.SafetySettings = new List<GeminiSafetySetting>(this.SafetySettings);
            clone.Tools = this.Tools.Select(t => t.Clone()).ToList();

            return clone;
        }
    }
}
