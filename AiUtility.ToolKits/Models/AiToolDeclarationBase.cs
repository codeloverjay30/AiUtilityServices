namespace AiUtility.ToolKits.Models
{
    public abstract class AiToolDeclarationBase
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // 使用 object 或泛型，因為各家 Schema 結構最容易在這裡產生分歧
        public object Parameters { get; set; } = new();
    }
}
