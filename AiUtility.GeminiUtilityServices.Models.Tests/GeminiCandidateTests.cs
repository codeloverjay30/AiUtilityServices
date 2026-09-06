using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;
using System.Text.Json;

namespace AiUtility.GeminiUtilityServices.Models.Tests
{
    public class GeminiCandidateTests
    {
        [Fact]
        public void Deserialize_WhenFinishReasonExists_ShouldMapValue()
        {
            // Arrange
            const string json =
                """
                {
                    "content": {
                        "role": "model",
                        "parts": [
                            {
                                "text": "Done"
                            }
                        ]
                    },
                    "finishReason": "STOP",
                    "index": 0
                }
                """;

            // Act
            var result =
                JsonSerializer.Deserialize<GeminiCandidate>(
                    json,
                    AiUtility.Common.Options.JsonOptions.DefaultOptions);

            // Assert
            result.Should().NotBeNull();
            result!.FinishReason.Should().Be("STOP");
            result.Index.Should().Be(0);
            result.Content.Should().NotBeNull();
        }

        [Fact]
        public void DeepClone_ShouldNotShareContentInstance()
        {
            // Arrange
            var sut = new GeminiCandidate
            {
                FinishReason = "STOP",
                Index = 0,
                Content = new GeminiMessage()
            };

            // Act
            var clone = sut.DeepClone();

            // Assert
            clone.Should().NotBeSameAs(sut);
            clone.Content.Should().NotBeSameAs(sut.Content);
            clone.FinishReason.Should().Be("STOP");
            clone.Index.Should().Be(0);
        }
    }
}