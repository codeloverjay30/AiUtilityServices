namespace AiUtility.GeminiKits.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class , AllowMultiple = true)]
    public class GeminiToolAttribute : Attribute
    {
        public string Description { get; set; }
        // 可以加入類別區分，例如支援原本其他的 Data Annotation 邏輯
        public string? Category { get; set; }
    }
}
