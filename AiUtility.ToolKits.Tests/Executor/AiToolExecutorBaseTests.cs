using System.Reflection;
using AiUtility.ToolKits.Abstractions;
using AiUtility.ToolKits.Executor;
using FluentAssertions;
using Moq;
using TypeUtilityServices;

namespace AiUtility.ToolKits.Tests.Executor;

public sealed class AiToolExecutorBaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenInstanceFactoryReturnsInstance_ShouldInvokeToolOnThatInstance()
    {
        // Arrange
        var tool = new TestTool();

        MethodInfo methodInfo =
            typeof(TestTool).GetMethod(
                nameof(TestTool.Execute))
            ?? throw new InvalidOperationException(
                "The test tool method could not be found.");

        Func<object?, object?[]?, object?> fastInvoke =
            (instance, arguments) =>
            {
                var target =
                    instance.Should()
                        .BeOfType<TestTool>()
                        .Subject;

                return target.Execute();
            };

        var metadata =
            new TestToolMetadata(
                nameof(TestTool.Execute),
                methodInfo,
                methodInfo.GetParameters(),
                fastInvoke,
                () => tool);

        var registry =
            new Mock<IToolRegistry<TestToolMetadata, TestToolAttribute>>(
                MockBehavior.Strict);

        TestToolMetadata? registeredMetadata = metadata;

        registry
            .Setup(x => x.TryGetTool(
                nameof(TestTool.Execute),
                out registeredMetadata))
            .Returns(true);

        var typeUtilityService =
            new Mock<ITypeUtilityService>(
                MockBehavior.Strict);

        var sut =
            new TestToolExecutor(
                registry.Object,
                typeUtilityService.Object);

        var arguments =
            new Dictionary<string, object>();

        // Act
        object? result =
            await sut.ExecuteAsync(
                nameof(TestTool.Execute),
                arguments,
                CancellationToken.None);

        // Assert
        result.Should().Be("executed");
        tool.InvocationCount.Should().Be(1);

        registry.Verify(
            x => x.TryGetTool(
                nameof(TestTool.Execute),
                out registeredMetadata),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInstanceFactoryReturnsNull_ForInstanceMethod_ShouldNotReachFastInvokeWithNullInstance()
    {
        // Arrange
        MethodInfo methodInfo =
            typeof(TestTool).GetMethod(
                nameof(TestTool.Execute))
            ?? throw new InvalidOperationException(
                "The test tool method could not be found.");

        Func<object?, object?[]?, object?> fastInvoke =
            (instance, arguments) =>
            {
                instance.Should().NotBeNull();

                return null;
            };

        var metadata =
            new TestToolMetadata(
                nameof(TestTool.Execute),
                methodInfo,
                methodInfo.GetParameters(),
                fastInvoke,
                () => null!);

        var registry =
            new Mock<IToolRegistry<TestToolMetadata, TestToolAttribute>>(
                MockBehavior.Strict);

        TestToolMetadata? registeredMetadata = metadata;

        registry
            .Setup(x => x.TryGetTool(
                nameof(TestTool.Execute),
                out registeredMetadata))
            .Returns(true);

        var typeUtilityService =
            new Mock<ITypeUtilityService>(
                MockBehavior.Strict);

        var sut =
            new TestToolExecutor(
                registry.Object,
                typeUtilityService.Object);

        var arguments =
            new Dictionary<string, object>();

        // Act
        Func<Task> act =
            async () =>
                await sut.ExecuteAsync(
                    nameof(TestTool.Execute),
                    arguments,
                    CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*instance*");
    }

    [Fact]
public async Task ExecuteAsync_WhenInstanceFactoryIsMissing_ForInstanceMethod_ShouldThrow()
{
    // Arrange
    MethodInfo methodInfo =
        typeof(TestTool).GetMethod(nameof(TestTool.Execute))
        ?? throw new InvalidOperationException(
            "The test tool method could not be found.");

    var metadata =
        new TestToolMetadata(
            nameof(TestTool.Execute),
            methodInfo,
            methodInfo.GetParameters(),
            (_, _) => "should not execute",
            instanceFactory: null);

    var registry =
        new Mock<IToolRegistry<TestToolMetadata, TestToolAttribute>>(
            MockBehavior.Strict);

    TestToolMetadata? registeredMetadata = metadata;

    registry
        .Setup(x => x.TryGetTool(
            nameof(TestTool.Execute),
            out registeredMetadata))
        .Returns(true);

    var typeUtilityService =
        new Mock<ITypeUtilityService>(MockBehavior.Strict);

    var sut =
        new TestToolExecutor(
            registry.Object,
            typeUtilityService.Object);

    // Act
    Func<Task> act = async () =>
        await sut.ExecuteAsync(
            nameof(TestTool.Execute),
            new Dictionary<string, object>(),
            CancellationToken.None);

    // Assert
    await act.Should()
        .ThrowAsync<InvalidOperationException>()
        .WithMessage("*no instance factory is configured*");
}

    [Fact]
    public async Task ExecuteAsync_WhenMethodIsStatic_ShouldNotInvokeInstanceFactory()
    {
        // Arrange
        MethodInfo methodInfo =
            typeof(TestTool).GetMethod(nameof(TestTool.ExecuteStatic))
            ?? throw new InvalidOperationException(
                "The static test tool method could not be found.");

        var factoryInvocationCount = 0;

        var metadata =
            new TestToolMetadata(
                nameof(TestTool.ExecuteStatic),
                methodInfo,
                methodInfo.GetParameters(),
                (instance, _) =>
                {
                    instance.Should().BeNull();
                    return "static executed";
                },
                () =>
                {
                    factoryInvocationCount++;
                    return new TestTool();
                });

        var registry =
            new Mock<IToolRegistry<TestToolMetadata, TestToolAttribute>>(
                MockBehavior.Strict);

        TestToolMetadata? registeredMetadata = metadata;

        registry
            .Setup(x => x.TryGetTool(
                nameof(TestTool.ExecuteStatic),
                out registeredMetadata))
            .Returns(true);

        var typeUtilityService =
            new Mock<ITypeUtilityService>(MockBehavior.Strict);

        var sut =
            new TestToolExecutor(
                registry.Object,
                typeUtilityService.Object);

        // Act
        object? result =
            await sut.ExecuteAsync(
                nameof(TestTool.ExecuteStatic),
                new Dictionary<string, object>(),
                CancellationToken.None);

        // Assert
        result.Should().Be("static executed");
        factoryInvocationCount.Should().Be(0);
    }


    public sealed class TestToolExecutor(
        IToolRegistry<TestToolMetadata, TestToolAttribute> registry,
        ITypeUtilityService typeUtilityService)
        : AiToolExecutorBase<TestToolMetadata, TestToolAttribute>(
            registry,
            typeUtilityService);

    public sealed class TestTool
    {
        public int InvocationCount { get; private set; }

        public string Execute()
        {
            InvocationCount++;

            return "executed";
        }

        public static string ExecuteStatic()
        {
            return "static executed";
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestToolAttribute : Attribute;

    public sealed record TestToolMetadata
        : ToolMetadataBase
    {
        public TestToolMetadata(
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