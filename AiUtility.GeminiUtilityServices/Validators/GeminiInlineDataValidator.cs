using AiUtility.GeminiUtilityServices.Models;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.GeminiUtilityServices.Validators
{
    public class GeminiInlineDataValidator
        : AbstractValidator<GeminiInlineData>, IGeminiInlineDataValidator
    {
        public GeminiInlineDataValidator()
        {
            // 驗證 MimeType 格式
            RuleFor(x => x.MimeType)
                .Matches(@"^image\/(jpeg|png|webp|heic|heif)$")
                .WithMessage(AiUtility.AiBaseUtilityServices.Consts.Constants.Constraints.UnsupportedFormat.UNSUPPORTED_IMAGE_FORMAT);
        }
    }
}
