using AiUtility.ToolKits.Abstractions;
using AiUtility.ToolKits.Executor;
using FluentAssertions;
using Moq;
using System.Reflection;
using TypeUtilityServices;
using Xunit;

namespace AiUtility.ToolKits.Tests.Executor;

public sealed class AiToolExecutorTests
{
    private readonly Mock<IToolRegistry<TestToolMetadata, TestToolAttribute>>
        _registryMock;

    private readonly Mock<ITypeUtilityService>
        _typeUtilityServiceMock;

    private readonly TestAiToolExecutor
        _sut;

    public AiToolExecutorTests()
    {
        _registryMock =
            new Mock<IToolRegistry<TestToolMetadata, TestToolAttribute>>(
                MockBehavior.Strict);

        _typeUtilityServiceMock =
            new Mock<ITypeUtilityService>(
                MockBehavior.Strict);

        _sut =
            new TestAiToolExecutor(
                _registryMock.Object,
                _typeUtilityServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenToolDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        TestToolMetadata? metadata = null;

        _registryMock
            .Setup(x => x.TryGetTool(
                "missing-tool",
                out metadata))
            .Returns(false);

        Func<Task> act =
            async () =>
                await _sut.ExecuteAsync(
                    "missing-tool",
                    new Dictionary<string, object>());

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("*missing-tool*");

        _registryMock.Verify(
            x => x.TryGetTool(
                "missing-tool",
                out metadata),
            Times.Once);

        _typeUtilityServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequiredArgumentIsMissing_ShouldThrowArgumentException()
    {
        var method =
            typeof(TestToolTarget)
                .GetMethod(
                    nameof(TestToolTarget.RequiredArgumentTool),
                    BindingFlags.Public |
                    BindingFlags.Instance);

        method.Should().NotBeNull();

        var metadata =
            CreateMetadata(
                nameof(TestToolTarget.RequiredArgumentTool),
                method!,
                new TestToolTarget());

        SetupTool(
            nameof(TestToolTarget.RequiredArgumentTool),
            metadata);

        Func<Task> act =
            async () =>
                await _sut.ExecuteAsync(
                    nameof(TestToolTarget.RequiredArgumentTool),
                    new Dictionary<string, object>());

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Missing required argument*value*");

        _typeUtilityServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOptionalArgumentIsMissing_ShouldUseDefaultValue()
    {
        var target =
            new TestToolTarget();

        var method =
            typeof(TestToolTarget)
                .GetMethod(
                    nameof(TestToolTarget.OptionalArgumentTool),
                    BindingFlags.Public |
                    BindingFlags.Instance);

        method.Should().NotBeNull();

        object?[]? capturedArguments = null;

        var metadata =
            CreateMetadata(
                nameof(TestToolTarget.OptionalArgumentTool),
                method!,
                target,
                (_, args) =>
                {
                    capturedArguments = args;

                    return method!.Invoke(
                        target,
                        args);
                });

        SetupTool(
            nameof(TestToolTarget.OptionalArgumentTool),
            metadata);

        var result =
            await _sut.ExecuteAsync(
                nameof(TestToolTarget.OptionalArgumentTool),
                new Dictionary<string, object>());

        result.Should()
            .Be("default-value");

        capturedArguments.Should()
            .NotBeNull();

        capturedArguments.Should()
            .ContainSingle();

        capturedArguments![0].Should()
            .Be("default-value");

        _typeUtilityServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenArgumentIsProvided_ShouldConvertAndPassArgument()
    {
        const string rawValue = "123";
        const int convertedValue = 123;

        var target =
            new TestToolTarget();

        var method =
            typeof(TestToolTarget)
                .GetMethod(
                    nameof(TestToolTarget.IntegerArgumentTool),
                    BindingFlags.Public |
                    BindingFlags.Instance);

        method.Should().NotBeNull();

        _typeUtilityServiceMock
            .Setup(x => x.SafeConvert(
                rawValue,
                typeof(int)))
            .Returns(convertedValue);

        object?[]? capturedArguments = null;

        var metadata =
            CreateMetadata(
                nameof(TestToolTarget.IntegerArgumentTool),
                method!,
                target,
                (_, args) =>
                {
                    capturedArguments = args;

                    return method!.Invoke(
                        target,
                        args);
                });

        SetupTool(
            nameof(TestToolTarget.IntegerArgumentTool),
            metadata);

        var result =
            await _sut.ExecuteAsync(
                nameof(TestToolTarget.IntegerArgumentTool),
                new Dictionary<string, object>
                {
                    ["value"] = rawValue
                });

        result.Should()
            .Be(convertedValue);

        capturedArguments.Should()
            .NotBeNull();

        capturedArguments.Should()
            .ContainSingle();

        capturedArguments![0].Should()
            .Be(convertedValue);

        _typeUtilityServiceMock.Verify(
            x => x.SafeConvert(
                rawValue,
                typeof(int)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenToolAcceptsCancellationToken_ShouldInjectProvidedToken()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var expectedToken =
            cancellationTokenSource.Token;

        var target =
            new TestToolTarget();

        var method =
            typeof(TestToolTarget)
                .GetMethod(
                    nameof(TestToolTarget.CancellationTokenTool),
                    BindingFlags.Public |
                    BindingFlags.Instance);

        method.Should().NotBeNull();

        object?[]? capturedArguments = null;

        var metadata =
            CreateMetadata(
                nameof(TestToolTarget.CancellationTokenTool),
                method!,
                target,
                (_, args) =>
                {
                    capturedArguments = args;

                    return method!.Invoke(
                        target,
                        args);
                });

        SetupTool(
            nameof(TestToolTarget.CancellationTokenTool),
            metadata);

        var result =
            await _sut.ExecuteAsync(
                nameof(TestToolTarget.CancellationTokenTool),
                new Dictionary<string, object>(),
                expectedToken);

        result.Should()
            .BeOfType<bool>()
            .Which.Should()
            .BeTrue();

        capturedArguments.Should()
            .NotBeNull();

        capturedArguments.Should()
            .ContainSingle();

        capturedArguments![0].Should()
            .Be(expectedToken);

        _typeUtilityServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenToolReturnsSynchronousValue_ShouldReturnValue()
    {
        var target =
            new TestToolTarget();

        var method =
            typeof(TestToolTarget)
                .GetMethod(
                    nameof(TestToolTarget.SynchronousTool),
                    BindingFlags.Public |
                    BindingFlags.Instance);

        method.Should().NotBeNull();

        var metadata =
            CreateMetadata(
                nameof(TestToolTarget.SynchronousTool),
                method!,
                target);

        SetupTool(
            nameof(TestToolTarget.SynchronousTool),
            metadata);

        var result =
            await _sut.ExecuteAsync(
                nameof(TestToolTarget.SynchronousTool),
                new Dictionary<string, object>());

        result.Should()
            .Be("sync-result");

        _typeUtilityServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenToolReturnsTaskOfT_ShouldReturnUnwrappedResult()
    {
        var target =
            new TestToolTarget();

        var method =
            typeof(TestToolTarget)
                .GetMethod(
                    nameof(TestToolTarget.AsyncTool),
                    BindingFlags.Public |
                    BindingFlags.Instance);

        method.Should().NotBeNull();

        var metadata =
            CreateMetadata(
                nameof(TestToolTarget.AsyncTool),
                method!,
                target);

        SetupTool(
            nameof(TestToolTarget.AsyncTool),
            metadata);

        var result =
            await _sut.ExecuteAsync(
                nameof(TestToolTarget.AsyncTool),
                new Dictionary<string, object>());

        result.Should()
            .Be("async-result");

        _typeUtilityServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationAlreadyRequested_ShouldThrowOperationCanceledException()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        Func<Task> act =
            async () =>
                await _sut.ExecuteAsync(
                    "any-tool",
                    new Dictionary<string, object>(),
                    cancellationTokenSource.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        _registryMock.VerifyNoOtherCalls();
        _typeUtilityServiceMock.VerifyNoOtherCalls();
    }

    private void SetupTool(
        string functionName,
        TestToolMetadata metadata)
    {
        TestToolMetadata? outputMetadata =
            metadata;

        _registryMock
            .Setup(x => x.TryGetTool(
                functionName,
                out outputMetadata))
            .Returns(true);
    }

    private static TestToolMetadata CreateMetadata(
        string functionName,
        MethodInfo method,
        object instance,
        Func<object?, object?[]?, object?>? fastInvoke = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            functionName);

        ArgumentNullException.ThrowIfNull(
            method);

        ArgumentNullException.ThrowIfNull(
            instance);

        return new TestToolMetadata(
            functionName,
            method,
            method.GetParameters(),
            fastInvoke
                ?? ((target, args) =>
                    method.Invoke(
                        target,
                        args)),
            () => instance,
            Array.Empty<Attribute>());
    }

    private sealed class TestAiToolExecutor
        : AiToolExecutor<TestToolMetadata, TestToolAttribute>
    {
        public TestAiToolExecutor(
            IToolRegistry<TestToolMetadata, TestToolAttribute> registry,
            ITypeUtilityService typeUtilityService)
            : base(
                registry,
                typeUtilityService)
        {
        }
    }

    public  sealed record TestToolMetadata
        : ToolMetadataBase
    {
        public TestToolMetadata(
            string name,
            MethodInfo methodInfo,
            ParameterInfo[] parameters,
            Func<object?, object?[]?, object?> fastInvoke,
            Func<object>? instanceFactory,
            IEnumerable<Attribute> methodAttributes)
            : base(
                name,
                methodInfo,
                parameters,
                fastInvoke,
                instanceFactory,
                methodAttributes)
        {
        }
    }

    [AttributeUsage(
        AttributeTargets.Method,
        AllowMultiple = false)]
    public sealed class TestToolAttribute
        : Attribute
    {
    }

    private sealed class TestToolTarget
    {
        public string RequiredArgumentTool(
            string value)
        {
            return value;
        }

        public string OptionalArgumentTool(
            string value = "default-value")
        {
            return value;
        }

        public int IntegerArgumentTool(
            int value)
        {
            return value;
        }

        public bool CancellationTokenTool(
            CancellationToken ct)
        {
            return ct.CanBeCanceled;
        }

        public string SynchronousTool()
        {
            return "sync-result";
        }

        public async Task<string> AsyncTool()
        {
            await Task.Yield();

            return "async-result";
        }
    }
}