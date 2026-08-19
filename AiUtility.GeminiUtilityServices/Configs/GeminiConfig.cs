extern alias MimeTypeAlias;
extern alias TypeAlias;

using MimeTypes = MimeTypeAlias::CommonConstants.MimeTypes;
using TypeConstants = TypeAlias::CommonConstants.Types.TypeConstants;

using AiUtility.GeminiUtilityServices.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Configs
{
    public class GeminiConfig
    {
        private const int MAX_OUTPUT_TOKENS = AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionSettings.DEFAULT_MAX_TOKENS;
        public GeminiGenerateRequest DefaultRequestConfig = new GeminiGenerateRequest
        {
            Prompt = string.Empty ,
            Contents = new() ,
            ResponseMimeType = MimeTypes.MimeTypeConstants.APPLICATION_JSON, // "application/json"
            Temperature = AiUtility.AiBaseUtilityServices.Consts.Constants.ExecutionSettings.DEFAULT_TEMPERATURE,
            MaxOutputTokens = MAX_OUTPUT_TOKENS ,
            ResponseSchema = new() ,
            SafetySettings = new() ,
            Tools = new()
        };
    }
}
