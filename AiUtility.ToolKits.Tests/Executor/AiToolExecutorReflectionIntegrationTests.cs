using System.Reflection;
using AiUtility.ToolKits.Abstractions;
using AiUtility.ToolKits.Executor;
using ExpressionTreeUtilityServices;
using FluentAssertions;
using Moq;
using ReflectionUtilityServices;
using TypeUtilityServices;

namespace AiUtility.ToolKits.Tests.Executor;

public sealed class AiToolExecutorReflectionIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenInstanceToolIsRegistered_ShouldInvokeRealReflectionDelegateOnRegisteredInstance()
    {
        // Arrange
        var tool = new IntegrationTestTool();

        MethodInfo methodInfo =
            typeof(IntegrationTestTool).GetMethod(
                nameof(IntegrationTestTool.Wait))
            ?? throw new InvalidOperationException(
                "The integration test tool method could not be found.");

        IExpressionTreeUtilityService expressionTreeUtilityService =
            new ExpressionTreeUtilityService();

        IReflectionUtilityService reflectionUtilityService =
            new ReflectionUtilityService(
                expressionTreeUtilityService);

        reflectionUtilityService.AddFastDelegate(methodInfo);

        Func<object, object?[]?, object?> fastInvoke =
            reflectionUtilityService.FastInvoke
            ?? throw new InvalidOperationException(
                "The reflection utility service did not create a fast delegate.");

        var metadata =
            new IntegrationTestToolMetadata(
                nameof(IntegrationTestTool.Wait),
                methodInfo,
                methodInfo.GetParameters(),
                fastInvoke,
                () => tool);


        var registry =
            new Mock<
                IToolRegistry<
                    IntegrationTestToolMetadata,
                    IntegrationTestToolAttribute>>(
                MockBehavior.Strict);

        IntegrationTestToolMetadata? registeredMetadata =
            metadata;

        registry
            .Setup(x => x.TryGetTool(
                nameof(IntegrationTestTool.Wait),
                out registeredMetadata))
            .Returns(true);

        var typeUtilityService =
            new Mock<ITypeUtilityService>(
                MockBehavior.Strict);

        typeUtilityService
            .Setup(x => x.SafeConvert(
                It.IsAny<object>(),
                typeof(int)))
            .Returns(
                (object value, Type _) =>
                    Convert.ToInt32(value));

        var sut =
            new IntegrationTestToolExecutor(
                registry.Object,
                typeUtilityService.Object);

        var arguments =
            new Dictionary<string, object>
            {
                ["milliseconds"] = 5,
            };

        // Act
        object? result =
            await sut.ExecuteAsync(
                nameof(IntegrationTestTool.Wait),
                arguments,
                CancellationToken.None);

        // Assert
        result.Should().Be(5);

        tool.InvocationCount.Should().Be(1);
        tool.LastMilliseconds.Should().Be(5);

        registry.Verify(
            x => x.TryGetTool(
                nameof(IntegrationTestTool.Wait),
                out registeredMetadata),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInstanceFactoryReturnsNull_ShouldNotInvokeRealReflectionDelegateWithNullInstance()
    {
        // Arrange
        MethodInfo methodInfo =
            typeof(IntegrationTestTool).GetMethod(
                nameof(IntegrationTestTool.Wait))
            ?? throw new InvalidOperationException(
                "The integration test tool method could not be found.");

        IExpressionTreeUtilityService expressionTreeUtilityService =
            new ExpressionTreeUtilityService();

        IReflectionUtilityService reflectionUtilityService =
            new ReflectionUtilityService(
                expressionTreeUtilityService);

        reflectionUtilityService.AddFastDelegate(methodInfo);

        Func<object, object?[]?, object?> fastInvoke =
            reflectionUtilityService.FastInvoke
            ?? throw new InvalidOperationException(
                "The reflection utility service did not create a fast delegate.");

        var metadata =
            new IntegrationTestToolMetadata(
                nameof(IntegrationTestTool.Wait),
                methodInfo,
                methodInfo.GetParameters(),
                fastInvoke,
                () => null!);

        var registry =
            new Mock<
                IToolRegistry<
                    IntegrationTestToolMetadata,
                    IntegrationTestToolAttribute>>(
                MockBehavior.Strict);

        IntegrationTestToolMetadata? registeredMetadata =
            metadata;

        registry
            .Setup(x => x.TryGetTool(
                nameof(IntegrationTestTool.Wait),
                out registeredMetadata))
            .Returns(true);

        var typeUtilityService =
            new Mock<ITypeUtilityService>(
                MockBehavior.Strict);

        typeUtilityService
            .Setup(x => x.SafeConvert(
                It.IsAny<object>(),
                typeof(int)))
            .Returns(
                (object value, Type _) =>
                    Convert.ToInt32(value));

        var sut =
            new IntegrationTestToolExecutor(
                registry.Object,
                typeUtilityService.Object);

        var arguments =
            new Dictionary<string, object>
            {
                ["milliseconds"] = 5,
            };

        // Act
        Func<Task> act =
            async () =>
                await sut.ExecuteAsync(
                    nameof(IntegrationTestTool.Wait),
                    arguments,
                    CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*instance*");
    }

    public sealed class IntegrationTestToolExecutor(
        IToolRegistry<
            IntegrationTestToolMetadata,
            IntegrationTestToolAttribute> registry,
        ITypeUtilityService typeUtilityService)
        : AiToolExecutorBase<
            IntegrationTestToolMetadata,
            IntegrationTestToolAttribute>(
                registry,
                typeUtilityService);

    public sealed class IntegrationTestTool
    {
        public int InvocationCount { get; private set; }

        public int? LastMilliseconds { get; private set; }

        public int Wait(int milliseconds)
        {
            InvocationCount++;
            LastMilliseconds = milliseconds;

            return milliseconds;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IntegrationTestToolAttribute
        : Attribute;

    public sealed record IntegrationTestToolMetadata
    : ToolMetadataBase
    {
        public IntegrationTestToolMetadata(
            string functionName,
            MethodInfo methodInfo,
            ParameterInfo[] parameters,
            Func<object?, object?[]?, object?> fastInvoke,
            Func<object>? instanceFactory)
            : base(
                functionName,
                methodInfo,
                parameters,
                fastInvoke,
                instanceFactory,
                Array.Empty<Attribute>())
        {
        }
    }
}