using AiUtility.GeminiUtilityServices.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using static AiUtility.AiBaseUtilityServices.Consts.Constants;

namespace AiUtility.GeminiUtilityServices.Validators
{
    public class GeminiGenerateRequestValidator
        : AbstractValidator<GeminiGenerateRequest> , IGeminiGenerateRequestValidator
    {
        public GeminiGenerateRequestValidator()
        {

            // 驗證 content 不得為空 (至少要餵給資料給AI Model，Token內至少有文字或圖像或影像等資料)
            RuleFor(x => x.Contents)
                .NotEmpty().WithMessage(Constraints.ValueConstraints.CONTENT_MUST_BE_NONEMPTY);

            // 驗證 Temperature 範圍 (Gemini API 規範通常為 0.0 ~ 2.0)
            RuleFor(x => x.Temperature)
                .InclusiveBetween(0.0 , 2.0)
                .WithMessage(Constraints.ValueConstraints.TEMPERATURE_MUST_BETWEEN_ZERO_AND_TWO);

            // 驗證 MaxOutputTokens
            RuleFor(x => x.MaxOutputTokens)
                .GreaterThan(0)
                .LessThanOrEqualTo(ExecutionSettings.AVAILABLE_MAX_TOKENS) // 視模型限制而定
                .WithMessage(Constraints.ValueConstraints.MAX_OUTPUT_TOKENS_MUST_BETWEEN_ZERO_AND_AVAILABLE_MAX_OUTPUT_TOKENS);

        }
    }
}
