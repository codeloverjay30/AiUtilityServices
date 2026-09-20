using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Executor;
using AiUtility.GeminiKits.Registry;
using ExpressionTreeUtilityServices;
using FluentAssertions;
using ReflectionUtilityServices;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Tests.Registry;

public sealed class GeminiToolRegistryExecutionIntegrationTests
{
    [Fact]
    public void Register_WhenInstanceFactoryIsProvided_ShouldPreserveRegisteredInstance()
    {
        // Arrange
        var tool =
            new IntegrationTestTool();

        var registry =
            CreateRegistry();

        // Act
        registry.Register<IntegrationTestTool>(
            () => tool);

        var found =
            registry.TryGetTool(
                nameof(IntegrationTestTool.Wait),
                out var metadata);

        // Assert
        found.Should().BeTrue();

        metadata.Should().NotBeNull();

        metadata!.InstanceFactory
            .Should()
            .NotBeNull();

        object? registeredInstance =
            metadata.InstanceFactory!();

        registeredInstance
            .Should()
            .BeSameAs(tool);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInstanceToolIsRegistered_ShouldInvokeRegisteredInstanceThroughRealRegistry()
    {
        // Arrange
        var tool =
            new IntegrationTestTool();

        var registry =
            CreateRegistry();

        registry.Register<IntegrationTestTool>(
            () => tool);

        ITypeUtilityService typeUtilityService =
            new TypeUtilityService();

        var sut =
            new GeminiToolExecutor(
                registry,
                typeUtilityService);

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

        tool.InvocationCount
            .Should()
            .Be(1);

        tool.LastMilliseconds
            .Should()
            .Be(5);
    }

    private static GeminiToolRegistry CreateRegistry()
    {
        IExpressionTreeUtilityService expressionTreeUtilityService =
            new ExpressionTreeUtilityService();

        IReflectionUtilityService reflectionUtilityService =
            new ReflectionUtilityService(
                expressionTreeUtilityService);

        return new GeminiToolRegistry(
            reflectionUtilityService);
    }

    public sealed class IntegrationTestTool
    {
        public int InvocationCount { get; private set; }

        public int? LastMilliseconds { get; private set; }

        [GeminiTool]
        public int Wait(
            int milliseconds)
        {
            InvocationCount++;
            LastMilliseconds = milliseconds;

            return milliseconds;
        }
    }
}