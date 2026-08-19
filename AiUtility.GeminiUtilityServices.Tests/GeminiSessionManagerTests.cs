using AiUtility.AiBaseUtilityServices.Consts;
using AiUtility.AiBaseUtilityServices.Models;
using AiUtility.GeminiKits.Abstractions;
using AiUtility.GeminiUtilityServices.Models;
using AiUtility.GeminiUtilityServices.Services;
using AiUtility.ToolKits.Abstractions;
using CommonModels;
using LoggerFactoryUtilityServices;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Moq;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using TaskUtilityServices;
using ThreadLevelLockingUtilityServices;
using Xunit;
using Xunit.Abstractions;
using Constants = AiUtility.AiBaseUtilityServices.Consts.Constants;
using ITestOutputHelper = Xunit.Abstractions.ITestOutputHelper;

namespace AiUtility.GeminiUtilityServices.Tests
{
    public class GeminiSessionManagerTests
    {
        private readonly Mock<ILoggerFactoryBaseUtilityService> _mockLoggerFactory;
        private readonly Mock<IGeminiConversationManager> _mockConversationManager;
        private readonly Mock<IGeminiToolService> _mockToolService;
        private readonly Mock<IGeminiToolExecutor> _mockToolExecutor;
        private readonly Mock<ISemaphoreSlimService> _mockSemaphoreService;
        private readonly Mock<SemaphoreSlim> _mockSemaphore;
        private readonly ITestOutputHelper _output;
        private static readonly JsonSerializerOptions _options = AiUtility.Common.Options.JsonOptions.DefaultOptions;
        public GeminiSessionManagerTests(ITestOutputHelper output)
        {
            _output = output;

            _mockLoggerFactory = new Mock<ILoggerFactoryBaseUtilityService>();
            _mockLoggerFactory.Setup(x => x.Logger).Returns(new Mock<ILogger>().Object);

            _mockConversationManager = new Mock<IGeminiConversationManager>();
            _mockToolService = new Mock<IGeminiToolService>();

            // _mockToolExecutor = new Mock<IGeminiToolExecutor>();
            _mockToolExecutor = new Mock<IGeminiToolExecutor>(MockBehavior.Strict); // use strict mode

            // 模擬 Semaphore
            _mockSemaphoreService = new Mock<ISemaphoreSlimService>();
            //_mockSemaphore = new Mock<SemaphoreSlim>(1 , 1);
            //_mockSemaphoreService.Setup(x => x.NormalTaskSemaphore).Returns(_mockSemaphore.Object);
            _mockSemaphoreService.Setup(x => x.LockWithTimeoutValueAsync(
                It.IsAny<CancellationToken>() ,
                It.IsAny<TimeSpan>() ,
                It.IsAny<bool>()
            ))
            .ReturnsAsync(new Mock<IDisposable>().Object);
        }

        /// <summary>
        /// Utility method:
        /// Print <paramref name="results"/>
        /// </summary>
        /// <param name="results"></param>
        private void PrintResults(StatusJsonModels results)
        {
            _output.WriteLine($"results:{JsonSerializer.Serialize(results)}");
            _output.WriteLine($"results.StatusList:{JsonSerializer.Serialize(results.StatusList)}");
            results.StatusList.ForEach(statusModel =>
            {
                _output.WriteLine($"statusModel:{JsonSerializer.Serialize(statusModel, _options)}");
            });
        }

        private void ThrowException(
            StatusJsonModels results,
            string expectedText
        )
        {
            // 利用您已有的 JSON 序列化設定，產生漂亮的多行 JSON 字串
            string jsonDetail = JsonSerializer.Serialize(results.StatusList , _options);

            throw new Xunit.Sdk.XunitException(
                $"{Environment.NewLine}斷言失敗！未能在結果清單中找到預期字串。" +
                $"{Environment.NewLine}預期值: {expectedText}" +
                $"{Environment.NewLine}實際清單內容: {jsonDetail}");
        }
        private void AssertJsonSerializedResults(
            StatusJsonModels results,
            string expectedText
        )
        {
            if(!results.StatusList.Any(r => r.Result?.Contains(expectedText) == true))
            {
                ThrowException(results , expectedText);
            }
        }
        private void AssertJsonSerializedErrorMessage(
            StatusJsonModels results,
            string expectedText
        )
        {
            if(results.IsAllSuccess || !results.StatusList.Any(r => r.ErrorMessage?.Contains(expectedText) == true))
            {
                ThrowException(results , expectedText);
            }
        }



        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldExecuteMultipleToolsInParallel()
        {
            // Arrange
            var request = new GeminiGenerateRequest();
            var userTask = "同時幫我打開燈並調低空調".AsMemory();

            string [ ] input = { "{\"temp\":\"24\"" };
            string jsonString = JsonSerializer.Serialize(input);
            using(JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                JsonElement root = doc.RootElement.Clone();
                // 1. 模擬第一輪回傳：包含兩個 Function Calls (燈、空調)
                var firstResponse = new GeminiResponse
                {
                    Candidates = new List<GeminiCandidate>
                    {
                        new GeminiCandidate
                        {
                            Content = new GeminiMessage
                            {
                                Role = Constants.AiApi.GeminiAiStudio.AiSchema.Roles.MODEL, // "model"
                                Parts = new List<GeminiPart>
                                {
                                    new GeminiPart
                                    {
                                        FunctionCall = new GeminiFunctionCall
                                        {
                                            Name = "TurnOnLight",
                                            Args = new Dictionary<string, JsonElement>()
                                        }
                                    },
                                    new GeminiPart
                                    {
                                        FunctionCall = new GeminiFunctionCall
                                        {
                                            Name = "SetTemperature",
                                            Args = new Dictionary<string, JsonElement>
                                            {
                                                ["temp"] = root
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                // 2. 模擬第二輪回傳：工具執行完後的最終文字回應
                var finalResponse = new GeminiResponse
                {
                    Candidates = new List<GeminiCandidate>
                    {
                        new GeminiCandidate
                        {
                            Content = new GeminiMessage
                            {
                                Role = Constants.AiApi.GeminiAiStudio.AiSchema.Roles.MODEL, // "model"
                                Parts = new List<GeminiPart> { new GeminiPart { Text = "好的，燈已打開，空調已調至24度。" } }
                            }
                        }
                    }
                };

                // 設定 Mock 序列
                _mockConversationManager.SetupSequence(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>() ,
                    It.IsAny<ReadOnlyMemory<char>>(),
                    It.IsAny<AiExecutionSettings>() ,
                    It.IsAny<CancellationToken>()

                ))
                    .ReturnsAsync(firstResponse)
                    .ReturnsAsync(finalResponse);

                _mockToolExecutor.Setup(x => x.ExecuteAsync(
                    It.IsAny<string>() ,
                    It.IsAny<IDictionary<string , object>>(),
                    It.IsAny<CancellationToken>()
                ))
                    .ReturnsAsync(new { status = "success" });

                var manager = new GeminiSessionManager(
                    _mockLoggerFactory.Object ,
                    _mockConversationManager.Object ,
                    _mockToolService.Object ,
                    _mockToolExecutor.Object ,
                    _mockSemaphoreService.Object
                );

                // Act
                var results = await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                    request,
                    userTask,
                    new AiExecutionSettings()
                );

                // Assert
                PrintResults( results );
                Assert.NotEmpty(results.StatusList);
                // 驗證是否回傳了最終文字
                // Assert.Contains(results.StatusList , r => r.Result.Contains("好的，燈已打開，空調已調至24度。"));
                AssertJsonSerializedResults(results , "好的，燈已打開，空調已調至24度。");

                // 驗證是否正確呼叫了兩次 SendMessageAsync (一次 userTask, 一次 function 結果)
                _mockConversationManager.Verify(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>() ,
                    It.IsAny<ReadOnlyMemory<char>>() ,
                    It.IsAny<AiExecutionSettings>() ,
                    It.IsAny<CancellationToken>()
                ) , Times.Exactly(2));

                // 驗證工具是否都被執行了
                _mockToolExecutor.Verify(x => x.ExecuteAsync(
                    "TurnOnLight" ,
                    It.IsAny<IDictionary<string , object>>(),
                    It.IsAny<CancellationToken>()
                ) , Times.Once);
                _mockToolExecutor.Verify(x => x.ExecuteAsync(
                    "SetTemperature" ,
                    It.IsAny<IDictionary<string , object>>(),
                    It.IsAny<CancellationToken>()
                ) , Times.Once);
            }
        }

        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldStopAtMaxSteps()
        {
            // Arrange: 模擬 AI 陷入死循環，不斷要求執行同一個工具
            var request = new GeminiGenerateRequest();
            var loopResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Role = Constants.AiApi.GeminiAiStudio.AiSchema.Roles.MODEL, // "model"
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart
                                {
                                    FunctionCall = new GeminiFunctionCall { Name = "LoopTool" }
                                }
                            }
                        }
                    }
                }
            };

            _mockConversationManager.Setup(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<ReadOnlyMemory<char>>(),
                It.IsAny<AiExecutionSettings>(),
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(loopResponse);

            _mockToolExecutor.Setup(x => x.ExecuteAsync(
                It.IsAny<string>() ,
                It.IsAny<Dictionary<string , object>>(),
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(new { status = "processing" }); // [cite1.1]

            var manager = new GeminiSessionManager(
                _mockLoggerFactory.Object ,
                _mockConversationManager.Object ,
                _mockToolService.Object ,
                _mockToolExecutor.Object ,
                _mockSemaphoreService.Object
            );

            // Act
            var results = await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(request , "一直跑".AsMemory(), new AiExecutionSettings());
            PrintResults(results);
            // Assert
            Assert.NotEmpty(results.StatusList);

            AssertJsonSerializedResults(results , "processing"); // 檢查是否包含回傳結果 (設定於[cite1.1])

            // 驗證是否呼叫了 11 次 SendMessage (1 次初次 + 10 次 Loop)
            _mockConversationManager.Verify(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<ReadOnlyMemory<char>>(),
                It.IsAny<AiExecutionSettings>()
            ) , Times.AtLeast(10));
        }

        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldHandlePartialToolFailures()
        {
            // Arrange
            var request = new GeminiGenerateRequest();
            var firstResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "ToolA" } },
                                new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "ToolB" } }
                            }
                        }
                    }
                }
            };
            var finalResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart> { new GeminiPart { Text = "處理完畢" } }
                        }
                    }
                }
            };

            _mockConversationManager.SetupSequence(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>() ,
                    It.IsAny<ReadOnlyMemory<char>>() ,
                    It.IsAny<AiExecutionSettings>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(firstResponse)
                .ReturnsAsync(finalResponse);

            // 模擬 ToolA 成功，ToolB 拋出例外
            _mockToolExecutor.Setup(x => x.ExecuteAsync(
                "ToolA" ,
                It.IsAny<Dictionary<string , object>>() ,
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(new { result = "OK" });

            _mockToolExecutor.Setup(x => x.ExecuteAsync(
                "ToolB" ,
                It.IsAny<Dictionary<string , object>>() ,
                It.IsAny<CancellationToken>()
            ))
                .ThrowsAsync(new Exception("設備連線中斷"));

            var manager = new GeminiSessionManager(
                _mockLoggerFactory.Object ,
                _mockConversationManager.Object ,
                _mockToolService.Object ,
                _mockToolExecutor.Object ,
                _mockSemaphoreService.Object
            );

            // Act
            var results = await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                request ,
                "測試部分失敗".AsMemory(),
                new AiExecutionSettings()
            );

            // Assert
            var toolAStatus = results.StatusList.FirstOrDefault(r => r.DataSource.Contains("ToolA"));
            var toolBStatus = results.StatusList.FirstOrDefault(r => r.DataSource.Contains("ToolB"));

            Assert.NotNull(toolAStatus);
            Assert.NotNull(toolBStatus);

            Assert.True(toolAStatus.IsSuccess);
            Assert.False(toolBStatus.IsSuccess);
            Assert.Contains("設備連線中斷" , toolBStatus.ErrorMessage);

            // 驗證是否還是呼叫了兩次 SendMessage (即使有工具失敗，也要把錯誤傳回給 AI)
            _mockConversationManager.Verify(x => x.SendMessageAsync(
                    It.IsAny<GeminiGenerateRequest>() ,
                    It.IsAny<ReadOnlyMemory<char>>() ,
                    It.IsAny<AiExecutionSettings>(),
                    It.IsAny<CancellationToken>()
                ) ,
                Times.Exactly(2)
            );
        }

        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldCancelWhenTokenTriggered()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var request = new GeminiGenerateRequest();

            _mockConversationManager.Setup(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<ReadOnlyMemory<char>>() ,
                It.IsAny<AiExecutionSettings>() ,
                It.IsAny<CancellationToken>()
            ))
                .ThrowsAsync(new OperationCanceledException(cts.Token)); // 直接模擬拋出

            var manager = new GeminiSessionManager(
                _mockLoggerFactory.Object ,
                _mockConversationManager.Object ,
                _mockToolService.Object ,
                _mockToolExecutor.Object ,
                _mockSemaphoreService.Object
            );

            // Act & Assert
            var task = manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                request ,
                "取消測試".AsMemory() ,
                new AiExecutionSettings(),
                cts.Token
            );
            cts.Cancel(); // 立即取消
            _output.WriteLine($"This task was cancelled.");
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
        }

        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldHandlePartialFailures()
        {
            // Arrange
            var request = new GeminiGenerateRequest();
            var mixedResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "SuccessTool" } },
                                new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "FailTool" } }
                            }
                        }
                    }
                }
            };

            _mockConversationManager.SetupSequence(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<ReadOnlyMemory<char>>() ,
                It.IsAny<AiExecutionSettings>() ,
                It.IsAny<CancellationToken>()
             ))
                .ReturnsAsync(mixedResponse)
                .ReturnsAsync(
                    new GeminiResponse
                    {
                        Candidates = new List<GeminiCandidate>
                        {
                            new GeminiCandidate
                            {
                                Content = new GeminiMessage
                                {
                                    Parts = new List<GeminiPart> { new GeminiPart { Text = "Done" } }
                                }
                            }
                        }
                    });

            _mockToolExecutor.Setup(x => x.ExecuteAsync(
                "SuccessTool" ,
                It.IsAny<Dictionary<string , object>>() ,
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(new { status = "ok" });

            _mockToolExecutor.Setup(x => x.ExecuteAsync(
                "FailTool" ,
                It.IsAny<Dictionary<string , object>>() ,
                It.IsAny<CancellationToken>()
            ))
                .ThrowsAsync(new Exception("Device Offline"));

            var manager = new GeminiSessionManager(
                _mockLoggerFactory.Object ,
                _mockConversationManager.Object ,
                _mockToolService.Object ,
                _mockToolExecutor.Object ,
                _mockSemaphoreService.Object
            );

            // Act
            var results = await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                request ,
                "並行測試".AsMemory(),
                new AiExecutionSettings()
            );

            // Assert
            Assert.Contains(results.StatusList , r => r.IsSuccess && r.DataSource.Contains("SuccessTool"));
            Assert.Contains(results.StatusList , r => !r.IsSuccess && r.DataSource.Contains("FailTool"));
        }

        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldReportDetailedProgress()
        {
            // Arrange
            var request = new GeminiGenerateRequest();
            var settings = new AiExecutionSettings { MaxSteps = 5 };
            var progressList = new List<WorkflowProgress>();
            var progressMock = new Progress<WorkflowProgress>(p => progressList.Add(p));

            var response = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { Text = "完成" }
                            }
                        }
                    }
                }
            };

            _mockConversationManager.Setup(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>(),
                It.IsAny<string>() ,
                It.IsAny<AiExecutionSettings>() ,
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(response);

            var manager = new GeminiSessionManager(
                _mockLoggerFactory.Object ,
                _mockConversationManager.Object ,
                _mockToolService.Object ,
                _mockToolExecutor.Object ,
                _mockSemaphoreService.Object
            );

            // Act
            await manager.ExecuteWithToolSupportAsync(
                request ,
                "測試進度".AsMemory() ,
                settings ,
                CancellationToken.None ,
                progressMock
            );

            // 由於 Progress<T> 是非同步觸發，稍微等待一下確保 Callback 已執行
            await Task.Delay(100);

            // Assert
            Assert.Contains(progressList , p => p.CurrentAction.Contains("AI"));
            Assert.Contains(progressList , p => p.Percentage == 100);
        }

        /// <summary>
        /// 驗證傳入的 Metadata 是否能正確出現在進度回報中（這對多設備自動化至關重要）。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldReportProgressWithMetadata()
        {
            // Arrange
            var request = new GeminiGenerateRequest();
            var settings = new AiExecutionSettings
            {
                MaxSteps = 3 ,
                Metadata = new Dictionary<string , string> { { "DeviceId" , "Pixel_7_Pro" } }
            };

            // ------------------ 確保執行緒安全
            var reportedProgress = new ConcurrentBag<WorkflowProgress>(); // 改用 ConcurrentBag
            var progressMock = new Progress<WorkflowProgress>(p => reportedProgress.Add(p));

            _mockConversationManager.Setup(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<string>() ,
                It.IsAny<AiExecutionSettings>() ,
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(new GeminiResponse
                {
                    Candidates = new List<GeminiCandidate>
                    {
                        new GeminiCandidate
                        {
                            Content = new GeminiMessage
                            {
                                Parts = new List<GeminiPart>
                                {
                                    new GeminiPart { Text = "Done" }
                                }
                            }
                        }
                    }
                }
            );

            var manager = new GeminiSessionManager(_mockLoggerFactory.Object , _mockConversationManager.Object , _mockToolService.Object , _mockToolExecutor.Object , _mockSemaphoreService.Object);

            // Act
            await manager.ExecuteWithToolSupportAsync(
                request , 
                "Task".AsMemory() , 
                settings , 
                CancellationToken.None , 
                progressMock
            );

            // Assert
            Assert.All(reportedProgress , p => Assert.Equal(3 , p.MaxSteps));
            // 這裡如果你在 WorkflowProgress 有定義 Metadata 欄位則可驗證
        }

        /// <summary>
        /// 驗證當開啟 ForceSequentialToolExecution 時，工具是按順序一個個執行的，而非並行。
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldExecuteSequentially_WhenFlagIsSet()
        {
            // Arrange
            var settings = new AiExecutionSettings { ForceSequentialToolExecution = true };
            var request = new GeminiGenerateRequest();

            var multiToolResponse = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "Step1" } },
                                new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "Step2" } }
                            }
                        }
                    }
                }
            };

            _mockConversationManager.SetupSequence(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<ReadOnlyMemory<char>>() ,
                It.IsAny<AiExecutionSettings>() ,
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(multiToolResponse)
                .ReturnsAsync(
                    new GeminiResponse
                    {
                        Candidates = new List<GeminiCandidate>
                        {
                            new GeminiCandidate
                            {
                                Content = new GeminiMessage
                                {
                                    Parts = new List<GeminiPart>
                                    {
                                        new GeminiPart { Text = "Finish" }
                                    }
                                }
                            }
                        }
                    }
                );

            var callOrder = new List<string>();
            _mockToolExecutor.Setup(x => x.ExecuteAsync(
                It.IsAny<string>() ,
                It.IsAny<IDictionary<string , object>>() ,
                It.IsAny<CancellationToken>()
             ))
                .Callback<string , IDictionary<string , object> , CancellationToken>((name , args , ct) => callOrder.Add(name))
                .ReturnsAsync(new { status = "ok" });

            var manager = new GeminiSessionManager(
                _mockLoggerFactory.Object ,
                _mockConversationManager.Object ,
                _mockToolService.Object ,
                _mockToolExecutor.Object ,
                _mockSemaphoreService.Object
            );

            // Act
            await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                request ,
                "Sequential Test".AsMemory() ,
                settings
            );

            // Assert
            Assert.Equal(2 , callOrder.Count);
            Assert.Equal("Step1" , callOrder [ 0 ]);
            Assert.Equal("Step2" , callOrder [ 1 ]);
        }

        /// <summary>
        /// 驗證 ExecuteWithToolSupportAsync 是否會將錯誤訊息包裝在 FunctionResponse 中再次傳送給 SendMessageAsync
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldIncludeMetadataInProgress()
        {
            // Arrange
            var request = new GeminiGenerateRequest();
            var deviceId = "Pixel_7_Pro";
            var settings = new AiExecutionSettings
            {
                Metadata = new Dictionary<string , string> { { "DeviceId" , deviceId } }
            };

            // ------------------ 確保執行緒安全
            var reportedProgress = new ConcurrentBag<WorkflowProgress>(); // 改用 ConcurrentBag
            var progressMock = new Progress<WorkflowProgress>(p => reportedProgress.Add(p));
            var mockResponse =
                new GeminiResponse
                {
                    Candidates = new List<GeminiCandidate>
                    {
                        new GeminiCandidate
                        {
                            Content = new GeminiMessage
                            {
                                Parts = new List<GeminiPart> { new GeminiPart { Text = "Done" } }
                            }
                        }
                    }
                };
            var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // 模擬 AI 直接回傳文字結束任務
            _mockConversationManager.Setup(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<string>() ,
                It.IsAny<AiExecutionSettings>() ,
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(mockResponse)
                .Callback(() => Thread.Sleep(1000));

            var manager = new GeminiSessionManager(
                _mockLoggerFactory.Object ,
                _mockConversationManager.Object ,
                _mockToolService.Object ,
                _mockToolExecutor.Object ,
                _mockSemaphoreService.Object
            );

            // Act
            await manager.ExecuteWithToolSupportAsync(
                request , 
                "Task".AsMemory() , 
                settings , 
                CancellationToken.None , 
                progressMock
            );

            // Assert
            Assert.All(reportedProgress , p => Assert.Equal(deviceId , p.Metadata [ "DeviceId" ])); // 驗證 Metadata 是否成功透傳
        }

        /// <summary>
        /// 驗證 AiExecutionSettings 中定義的 Metadata（如 DeviceId）會出現在最終的執行結果中
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldPreserveMetadataInStatusList()
        {
            // Arrange
            var request = new GeminiGenerateRequest();
            var settings = new AiExecutionSettings { MaxSteps = 2 };
            settings.Metadata.Add("DeviceId" , "Emulator_5554");

            // 模擬 AI 回傳文字
            var response = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart> { new GeminiPart { Text = "Success" } }
                        }
                    }
                }
            };

            _mockConversationManager.Setup(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<string>() ,
                It.IsAny<AiExecutionSettings>() ,
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(response);

            var manager = new GeminiSessionManager(_mockLoggerFactory.Object , _mockConversationManager.Object , _mockToolService.Object , _mockToolExecutor.Object , _mockSemaphoreService.Object);

            // Act
            var results = await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                request , 
                "Test Task".AsMemory() , 
                settings
            );

            // Assert
            // 驗證產出的 StatusJsonModel 是否包含初始傳入的 Metadata
            Assert.All(results.StatusList , s => {
                Assert.True(s.Metadata.ContainsKey("DeviceId"));
                Assert.Equal("Emulator_5554" , s.Metadata [ "DeviceId" ]);
            });
        }

        [Fact]
        public async Task ExecuteWithToolSupportAsync_ShouldHandleToolTimeout()
        {
            // Arrange
            var settings = new AiExecutionSettings
            {
                ToolExecutionTimeout = TimeSpan.FromSeconds(1), //工具執行最多只能花一秒鐘 
            }; 
            var request = new GeminiGenerateRequest();

            var responseWithTool = new GeminiResponse
            {
                Candidates = new List<GeminiCandidate>
                {
                    new GeminiCandidate
                    {
                        Content = new GeminiMessage
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { FunctionCall = new GeminiFunctionCall { Name = "LongRunningTool" } }
                            }
                        }
                    }
                }
            };

            _mockConversationManager.SetupSequence(x => x.SendMessageAsync(
                It.IsAny<GeminiGenerateRequest>() ,
                It.IsAny<ReadOnlyMemory<char>>() ,
                It.IsAny<AiExecutionSettings>() ,
                It.IsAny<CancellationToken>()
            ))
                .ReturnsAsync(responseWithTool)
                .ReturnsAsync(
                new GeminiResponse
                {
                    Candidates = new List<GeminiCandidate>
                    {
                        new GeminiCandidate
                        {
                            Content = new GeminiMessage
                            {
                                Parts = new List<GeminiPart>
                                {
                                    new GeminiPart { Text = "Finish" }
                                }
                            }
                        }
                    }
                }
            );

            // 模擬工具執行會超過 1 秒
            _mockToolExecutor.Setup(x => x.ExecuteAsync(
                It.IsAny<string>() ,
                It.IsAny<Dictionary<string , object>>() ,
                It.IsAny<CancellationToken>()
            ))
                .Returns(async (string n , IDictionary<string , object> a , CancellationToken ct) => {
                    await Task.Delay(2000 , ct); // 工具會跑 2 秒，但設定只有 1 秒
                    return "Success";
                });

            var manager = new GeminiSessionManager(_mockLoggerFactory.Object , _mockConversationManager.Object , _mockToolService.Object , _mockToolExecutor.Object , _mockSemaphoreService.Object);

            // Act
            var results = await manager.ExecuteWithToolSupportAsync<WorkflowProgress>(
                request ,
                "Test Timeout".AsMemory() ,
                settings
            );

            // Assert
            PrintResults(results);
            var timeoutStatus = results.StatusList.FirstOrDefault(s => s.DataSource.Contains("LongRunningTool"));
            Assert.NotNull(timeoutStatus);
            Assert.False(timeoutStatus.IsSuccess);
            Assert.Contains("timeout" , timeoutStatus.OverallErrorMessage);
        }
    }
}
