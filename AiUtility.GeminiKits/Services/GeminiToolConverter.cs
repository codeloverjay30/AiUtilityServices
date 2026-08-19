using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Models;
using AiUtility.ToolKits.Consts;
using AiUtility.ToolKits.Services;
using EnumUtilityServices;
using JsonUtilityServices;

namespace AiUtility.GeminiKits.Services
{
    public class GeminiToolConverter(
        IJsonUtilityService jsonUtilityService,
        IEnumUtilityService enumUtilityService,
        string defaultDescription = AiToolConstants.DefaultDescription,
        string defaultParameterDescription = AiToolConstants.DefaultParameterDescription
    ) : AiToolConverterBase<GeminiToolAttribute,GeminiToolDeclaration,GeminiParameters,GeminiParameterProperty>
        (
            jsonUtilityService ,
            enumUtilityService,
            defaultDescription,
            defaultParameterDescription
        )
    {
        protected override string? GetDescriptionFromAttribute(GeminiToolAttribute? attr)
            => attr?.Description;
    }
}
