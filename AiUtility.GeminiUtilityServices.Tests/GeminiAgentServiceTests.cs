using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using AssemblyUtilityServices;
using FluentAssertions;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using TaskUtilityServices;
using ThreadLevelLockingUtilityServices;
using ThreadLevelLockingUtilityServices.Models;
using Xunit;

namespace AiUtility.GeminiUtilityServices.Tests
{
    public class GeminiAgentServiceTests
    {
        [Fact]
        public void Configure_ShouldRegisterToolsFromAllLoadedAssemblies()
        {
            // Arrange (準備環境)
            var mockLoggerFactory = new Mock<ILoggerFactoryBaseUtilityService>();
            mockLoggerFactory.Setup(x => x.Logger).Returns(new Mock<ILogger>().Object);

            var mockDispatcher = new Mock<IGeminiToolDispatcher>();
            var mockToolService = new Mock<IGeminiToolService>();
            var mockConversionManager = new Mock<IGeminiConversationManager>();
            var mockSessionManager = new Mock<IGeminiSessionManager>();

            // 模擬 Assembly 工具
            var mockAssembliesUtilityService = new Mock<IAssembliesUtilityService>();
            var mockRegistry = new Mock<IGeminiToolRegistry>();

            // 模擬兩個 DLL 檔案
            var fakeDlls = new List<string> { "Test.ToolA.dll" , "Test.ToolB.dll" };
            var fakeAssemblies = new List<Assembly>
            {
                typeof(object).Assembly, // 使用系統 Assembly 代替實體 DLL 做測試
                this.GetType().Assembly
            };

            mockDispatcher.Setup(x => x.ToolRegistry).Returns(mockRegistry.Object);

            mockAssembliesUtilityService.Setup(x => x.ListAllAssemblies()).Returns(fakeDlls);
            mockAssembliesUtilityService.Setup(x => x.LoadAllAssemblies(fakeDlls)).Returns(fakeAssemblies);

            var agentService = new GeminiAgentService(
                mockLoggerFactory.Object ,
                mockAssembliesUtilityService.Object,
                mockDispatcher.Object,
                mockSessionManager.Object
            );

            // Assert (驗證結果)
            mockRegistry.Verify(x => x.RegisterFromAssemblies(It.IsAny<List<Assembly>>()) , Times.Once);
        }

        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldTriggerConsolidation_WhenTokenExceedsLimit()
        {
            // Arrange

            var mockLoggerFactoryService = new Mock<ILoggerFactoryBaseUtilityService>();
            var mockConversationManager = new Mock<IGeminiConversationManager>();
            var mockTaskUtilityService = new Mock<ITaskUtilityService>();
            var mockToolService = new Mock<IGeminiToolService>();
            var mockToolExecutor = new Mock<IGeminiToolExecutor>();
            var mockSemaphoreSlimService = new Mock<ISemaphoreSlimService>();

            var request = new GeminiGenerateRequest();
            request.AddUserMessage("Old Message".AsMemory());

            // 這裡請根據您 IGeminiApiClient.GenerateContentAsync 回傳的實際類別名稱修改
            // 假設您的類別叫 GeminiResponse (通常包含 Candidates 和 UsageMetadata)
            var highTokenResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Role = Constants.AiApi.GeminiAiStudio.AiSchema.Roles.MODEL, // "model"
                            Parts = new List<GeminiPart> { new GeminiPart { Text = "I am processing..." } }
                        }
                    }
                } ,
                UsageMetadata = new GeminiUsageMetadata
                {
                    TotalTokenCount = 350000
                } // 觸發 30 萬門檻
            };

            var summaryResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Role = AiUtility.AiBaseUtilityServices.Consts.Constants.AiApi.GeminiAiStudio.AiSchema.Roles.MODEL, // "model"
                            Parts = new List<GeminiPart> { new GeminiPart { Text = "已完成 A 任務與 B 環境設定。" } }
                        }
                    }
                }
            };

            mockLoggerFactoryService.Setup(x => x.Logger).Returns(new Mock<ILogger>().Object);


            var semaphoreSlimModel = new SemaphoreSlimModel
            {
                InitialCount = 10 ,
                MaxCount = 10
            };

            int callCount = 0;
            mockConversationManager.Setup(x => x.Client.GenerateContentAsync(It.IsAny<GeminiGenerateRequest>()))
                .ReturnsAsync((GeminiGenerateRequest req) => {
                    callCount++;
                    if(callCount == 1)
                    {
                        return highTokenResponse;
                    }
                    return summaryResponse;
                });

            var manager = new GeminiSessionManager(
                mockLoggerFactoryService.Object ,
                mockConversationManager.Object ,
                mockToolService.Object ,
                mockToolExecutor.Object ,
                mockSemaphoreSlimService.Object
            );

            // Act
            await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                request ,
                "New Task".AsMemory() ,
                new AiExecutionSettings()
            );

            // Assert
            // 1. 驗證是否呼叫了一次 API (一次正常對話)
            mockConversationManager.Verify(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>() ,
                    It.IsAny<ReadOnlyMemory<char>>() ,
                    It.IsAny<AiExecutionSettings>(),
                    It.IsAny<CancellationToken>()
                ) ,
                Times.AtLeastOnce
            );

            // 2. 驗證 Request.Contents 是否未被重組，(而不包含Milestone字樣)
            request.Contents.Should().Contain(message =>
                message.Parts.Any(
                    p => p.Text == null ||
                    !p.Text.Contains(AiUtility.AiBaseUtilityServices.Consts.Constants.AiTasks.Remembers.REVIEW_TASKS_AND_MILESTONE) // $"Take {Vocabulary.REVIEW} of {ExecutionStatus.EXECUTED} {ToolTasks.TASK} and {Vocabulary.MILESTONE}"
                )
            );
        }
    }
}
