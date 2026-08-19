using System.Text.Json;
using static AiUtility.GeminiUtilityServices.Models.GeminiGenerateRequest;

namespace AiUtility.GeminiUtilityServices.Models.Tests;

public class GeminiGenerateRequestTests
{
    [Fact]
    public void Clone_ShouldCreateNewInstance_WithSameValues()
    {
        // Arrange: 準備原始物件並填入一些資料
        var original = new GeminiGenerateRequest
        {
            Prompt = "Original Prompt" ,
            SystemInstruction = "Be helpful" ,
            Temperature = 1.5 ,
            MaxOutputTokens = 1000
        };
        original.Contents.Add(new GeminiMessage { Role = "user" });
        original.SafetySettings.Add(new GeminiSafetySetting { Category = "HATE" });
        original.Tools.Add(new GeminiToolDeclarationWrapper());

        // Act: 執行 Clone
        var cloned = original.Clone();

        // Assert: 驗證屬性值是否相同
        Assert.NotSame(original , cloned); // 記憶體位址不應相同
        Assert.Equal(original.Prompt , cloned.Prompt);
        Assert.Equal(original.SystemInstruction , cloned.SystemInstruction);
        Assert.Equal(original.Temperature , cloned.Temperature);
        Assert.Equal(original.MaxOutputTokens , cloned.MaxOutputTokens);
    }

    [Fact]
    public void Clone_ShouldEnsureCollectionsAreIndependent_ToAvoidRaceCondition()
    {
        // Arrange
        var original = new GeminiGenerateRequest();
        original.Contents.Add(new GeminiMessage { Role = "user" , Parts = new List<GeminiPart> { new() { Text = "Initial" } } });
        original.Tools.Add(new GeminiToolDeclarationWrapper());

        // Act
        var cloned = original.Clone();

        // 修改 Cloned 物件的集合
        cloned.Contents.Add(new GeminiMessage { Role = "model" });
        cloned.Tools.Add(new GeminiToolDeclarationWrapper());

        // Assert: 驗證原始物件的集合數量不受影響
        Assert.Single(original.Contents);
        Assert.Single(original.Tools);
        Assert.Equal(2 , cloned.Contents.Count);
        Assert.Equal(2 , cloned.Tools.Count);

        // 驗證集合本身的引用不同
        Assert.NotSame(original.Contents , cloned.Contents);
        Assert.NotSame(original.Tools , cloned.Tools);
        Assert.NotSame(original.SafetySettings , cloned.SafetySettings);
    }

    [Fact]
    public void Clone_ShouldPerformDeepCopyOnTools_UsingWrapperShallowCopy()
    {
        // Arrange
        var original = new GeminiGenerateRequest();
        var toolWrapper = new GeminiToolDeclarationWrapper();
        toolWrapper.FunctionDeclarations.Add("OriginalFunction");
        original.Tools.Add(toolWrapper);

        // Act
        var cloned = original.Clone();

        // 修改 cloned 裡面的第一個工具的 FunctionDeclarations
        cloned.Tools [ 0 ].FunctionDeclarations.Add("NewFunction");

        // Assert: 驗證 original 內部的工具內容沒有被污染
        Assert.Single(original.Tools [ 0 ].FunctionDeclarations);
        Assert.Equal(2 , cloned.Tools [ 0 ].FunctionDeclarations.Count);

        // 驗證 Wrapper 實體也被更換了
        Assert.NotSame(original.Tools [ 0 ] , cloned.Tools [ 0 ]);
    }

    [Fact]
    public void ToGoogleApiRequest_ShouldReturnValidJsonStructure()
    {
        // Arrange
        var request = new GeminiGenerateRequest
        {
            Prompt = "Test" ,
            SystemInstruction = "Instruction" ,
            Temperature = 0.7 ,
            MaxOutputTokens = 500
        };

        // Act
        var apiRequest = request.ToGoogleApiRequest();

        // 將匿名物件序列化為 JSON 字串，這樣就沒有跨專案存取 internal 屬性的問題
        var json = JsonSerializer.Serialize(apiRequest);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Assert: 檢查 JSON 結構與欄位名稱是否符合 Google API 規範
        Assert.True(root.TryGetProperty("contents" , out _));

        // 驗證 system_instruction 結構
        var systemInstruction = root.GetProperty("system_instruction");
        Assert.Equal("Instruction" , systemInstruction.GetProperty("parts") [ 0 ].GetProperty("text").GetString());

        // 驗證 generationConfig 欄位名稱 (注意 Google API 使用 camelCase)
        var config = root.GetProperty("generationConfig");
        Assert.Equal(0.7 , config.GetProperty("temperature").GetDouble());
        Assert.Equal(500 , config.GetProperty("maxOutputTokens").GetInt32());
    }
}
