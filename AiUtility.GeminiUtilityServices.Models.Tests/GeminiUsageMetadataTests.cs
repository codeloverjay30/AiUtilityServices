using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;
using System.Text.Json;

namespace AiUtility.GeminiUtilityServices.Tests.Models;

public class GeminiUsageMetadataTests
{
    [Fact]
    public void Deserialize_WhenUsageMetadataContainsDetails_ShouldNormalizeUsage()
    {
        const string json =
            """
            {
                "promptTokenCount": 120,
                "promptTokensDetails": [
                    {
                        "modality": "TEXT",
                        "tokenCount": 80
                    },
                    {
                        "modality": "IMAGE",
                        "tokenCount": 40
                    }
                ],
                "candidatesTokenCount": 30,
                "candidatesTokensDetails": [
                    {
                        "modality": "TEXT",
                        "tokenCount": 30
                    }
                ],
                "cachedContentTokenCount": 0,
                "cacheTokensDetails": [],
                "thoughtsTokenCount": 10,
                "totalTokenCount": 160
            }
            """;

        var result =
            JsonSerializer.Deserialize<GeminiUsageMetadata>(
                json,
                AiUtility.Common.Options.JsonOptions.DefaultOptions);

        result.Should().NotBeNull();

        result!.Prompt.TokenCount.Should().Be(120);
        result.Prompt.Details.Should().HaveCount(2);

        result.Prompt.Details[0].Modality.Should().Be("TEXT");
        result.Prompt.Details[0].TokenCount.Should().Be(80);

        result.Candidates.TokenCount.Should().Be(30);
        result.Candidates.Details.Should().ContainSingle();

        result.Cache.TokenCount.Should().Be(0);
        result.Cache.Details.Should().BeEmpty();

        result.ThoughtsTokenCount.Should().Be(10);
        result.TotalTokenCount.Should().Be(160);
    }

    [Fact]
    public void Deserialize_WhenUnknownUsagePropertyExists_ShouldThrowJsonException()
    {
        const string json =
            """
        {
            "promptTokenCount": 10,
            "candidatesTokenCount": 5,
            "totalTokenCount": 15,
            "unexpectedUsageProperty": 123
        }
        """;

        Action act = () =>
            JsonSerializer.Deserialize<GeminiUsageMetadata>(
                json,
                AiUtility.Common.Options.JsonOptions.DefaultOptions);

        act.Should()
            .Throw<JsonException>()
            .WithMessage("*unexpectedUsageProperty*");
    }
}