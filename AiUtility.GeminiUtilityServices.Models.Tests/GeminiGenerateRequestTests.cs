using AiUtility.GeminiUtilityServices.Models;
using FluentAssertions;

namespace AiUtility.GeminiUtilityServices.Models.Tests
{
    public class GeminiGenerateRequestTests
    {
        [Fact]
        public void DeepClone_ShouldDeepCloneContents()
        {
            // Arrange
            var sut = new GeminiGenerateRequest
            {
                Prompt = "test prompt",
                Contents =
                [
                    new GeminiMessage
                    {
                        Role = "user",
                        Parts =
                        [
                            new GeminiPart
                            {
                                Text = "original"
                            }
                        ]
                    }
                ]
            };

            // Act
            var clone = sut.DeepClone();

            // Assert
            clone.Should().NotBeSameAs(sut);

            clone.Prompt.Should().Be("test prompt");

            clone.Contents.Should()
                .NotBeSameAs(sut.Contents);

            clone.Contents.Should()
                .HaveCount(1);

            clone.Contents[0].Should()
                .NotBeSameAs(sut.Contents[0]);

            clone.Contents[0].Parts.Should()
                .NotBeSameAs(sut.Contents[0].Parts);

            clone.Contents[0].Parts[0].Should()
                .NotBeSameAs(sut.Contents[0].Parts[0]);

            clone.Contents[0].Parts[0].Text.Should()
                .Be("original");
        }

        [Fact]
        public void DeepClone_WhenNestedContentChanges_ShouldNotModifyOriginal()
        {
            // Arrange
            var sut = new GeminiGenerateRequest
            {
                Contents =
                [
                    new GeminiMessage
                    {
                        Parts =
                        [
                            new GeminiPart
                            {
                                Text = "original"
                            }
                        ]
                    }
                ]
            };

            // Act
            var clone = sut.DeepClone();

            clone.Contents[0]
                .Parts[0]
                .Text = "changed";

            // Assert
            sut.Contents[0]
                .Parts[0]
                .Text.Should()
                .Be("original");

            clone.Contents[0]
                .Parts[0]
                .Text.Should()
                .Be("changed");
        }

        [Fact]
        public void DeepClone_ShouldDeepCloneMutableCollections()
        {
            // Arrange
            var sut = new GeminiGenerateRequest
            {
                SafetySettings =
                [
                    new GeminiSafetySetting
                    {
                        Category = "category",
                        Threshold = "threshold"
                    }
                ],
                Tools =
                [
                    new GeminiGenerateRequest.GeminiToolDeclarationWrapper
                    {
                        FunctionDeclarations =
                        [
                            "tool"
                        ]
                    }
                ]
            };

            // Act
            var clone = sut.DeepClone();

            // Assert
            clone.SafetySettings.Should()
                .NotBeSameAs(sut.SafetySettings);

            clone.SafetySettings[0].Should()
                .NotBeSameAs(sut.SafetySettings[0]);

            clone.Tools.Should()
                .NotBeSameAs(sut.Tools);

            clone.Tools[0].Should()
                .NotBeSameAs(sut.Tools[0]);

            clone.Tools[0].FunctionDeclarations.Should()
                .NotBeSameAs(
                    sut.Tools[0].FunctionDeclarations);
        }
    }
}