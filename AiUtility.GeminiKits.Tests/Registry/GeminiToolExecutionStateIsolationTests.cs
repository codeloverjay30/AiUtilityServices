using System.Net.NetworkInformation;
using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Registry;
using AiUtility.GeminiKits.Services;
using AiUtility.GeminiKits.TestFixtures.Models;
using AiUtility.GeminiKits.TestFixtures.Tools;
using ExpressionTreeUtilityServices;
using FluentAssertions;
using ReflectionUtilityServices;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Tests.Registry;

public sealed class GeminiToolExecutionStateIsolationTests
{
    [Fact]
    public async Task DispatchAsync_WhenFactoryStateChangesBeforeInvocation_ShouldResolveStateAtInvocationTime()
    {
        ExecutionState stateA =
            new("snapshot-A");

        ExecutionState stateB =
            new("snapshot-B");

        ExecutionState currentState =
            stateA;

        GeminiToolRegistry registry =
            CreateRegistry();

        registry.Register<StatefulExecutionTool>(
            () =>
                new StatefulExecutionTool(
                    currentState));

        currentState = stateB;

        GeminiToolDispatcher dispatcher =
            CreateDispatcher(
                registry);

        object? result =
            await dispatcher.DispatchAsync(
                nameof(
                    StatefulExecutionTool.GetSnapshotId),
                CreateEmptyArguments());

        result
            .Should()
            .Be("snapshot-B");
    }

    [Fact]
    public async Task DispatchAsync_WhenInvokedTwice_ShouldInvokeFactoryOncePerInvocation()
    {
        ExecutionState state =
            new("snapshot-A");

        int factoryInvocationCount = 0;

        GeminiToolRegistry registry =
            CreateRegistry();

        registry.Register<StatefulExecutionTool>(
            () =>
            {
                Interlocked.Increment(
                    ref factoryInvocationCount);

                return new StatefulExecutionTool(
                    state);
            });

        GeminiToolDispatcher dispatcher =
            CreateDispatcher(
                registry);

        object? firstResult =
            await dispatcher.DispatchAsync(
                nameof(
                    StatefulExecutionTool.GetSnapshotId),
                CreateEmptyArguments());

        object? secondResult =
            await dispatcher.DispatchAsync(
                nameof(
                    StatefulExecutionTool.GetSnapshotId),
                CreateEmptyArguments());

        firstResult
            .Should()
            .Be("snapshot-A");

        secondResult
            .Should()
            .Be("snapshot-A");

        factoryInvocationCount
            .Should()
            .Be(
                2,
                because:
                    "the registered instance factory must execute once for every tool invocation");
    }

    [Fact]
    public async Task DispatchAsync_WhenFactoryCapturesRegistrationState_ShouldRemainBoundToCapturedState()
    {
        ExecutionState registrationState =
            new("snapshot-A");

        GeminiToolRegistry registry =
            CreateRegistry();

        registry.Register<StatefulExecutionTool>(
            () =>
                new StatefulExecutionTool(
                    registrationState));

        GeminiToolDispatcher dispatcher =
            CreateDispatcher(
                registry);

        object? result =
            await dispatcher.DispatchAsync(
                nameof(
                    StatefulExecutionTool.GetSnapshotId),
                CreateEmptyArguments());

        result
            .Should()
            .Be(
                "snapshot-A",
                because:
                    "the factory explicitly captures the registration-owned state");
    }

    [Fact]
    public async Task DispatchAsync_WhenExecutionsShareMutableResolverState_ShouldExposeCrossExecutionContamination()
    {
        ExecutionState stateA =
            new("snapshot-A");

        ExecutionState stateB =
            new("snapshot-B");

        ExecutionState currentState =
            stateA;

        GeminiToolRegistry registry =
            CreateRegistry();

        registry.Register<StatefulExecutionTool>(
            () =>
                new StatefulExecutionTool(
                    currentState));

        GeminiToolDispatcher dispatcher =
            CreateDispatcher(
                registry);

        TaskCompletionSource executionAReady =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        TaskCompletionSource executionBReplacedState =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        Task<object?> executionA =
            Task.Run(
                async () =>
                {
                    currentState = stateA;

                    executionAReady.SetResult();

                    await executionBReplacedState.Task;

                    return await dispatcher.DispatchAsync(
                        nameof(
                            StatefulExecutionTool.GetSnapshotId),
                        CreateEmptyArguments());
                });

        Task<object?> executionB =
            Task.Run(
                async () =>
                {
                    await executionAReady.Task;

                    currentState = stateB;

                    executionBReplacedState.SetResult();

                    return await dispatcher.DispatchAsync(
                        nameof(
                            StatefulExecutionTool.GetSnapshotId),
                        CreateEmptyArguments());
                });

        object?[] results =
            await Task.WhenAll(
                executionA,
                executionB);

        results
            .Should()
            .HaveCount(2);

        results[0]
            .Should()
            .Be(
                "snapshot-B",
                because:
                    "execution B replaced the shared mutable state before execution A invoked its tool");

        results[1]
            .Should()
            .Be("snapshot-B");
    }

    [Fact]
    public async Task DispatchAsync_WhenResolverUsesExecutionLocalState_ShouldPreserveConcurrentExecutionAffinity()
    {
        AsyncLocal<ExecutionState?> executionState =
            new();

        GeminiToolRegistry registry =
            CreateRegistry();

        registry.Register<StatefulExecutionTool>(
            () =>
            {
                ExecutionState state =
                    executionState.Value
                    ?? throw new InvalidOperationException(
                        "No execution state is available.");

                return new StatefulExecutionTool(
                    state);
            });

        GeminiToolDispatcher dispatcher =
            CreateDispatcher(
                registry);

        TaskCompletionSource bothExecutionsReady =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        int readyExecutionCount = 0;

        async Task<object?> ExecuteAsync(
            ExecutionState state)
        {
            executionState.Value = state;

            try
            {
                int readyCount =
                    Interlocked.Increment(
                        ref readyExecutionCount);

                if (readyCount == 2)
                {
                    bothExecutionsReady.SetResult();
                }

                await bothExecutionsReady.Task;

                return await dispatcher.DispatchAsync(
                    nameof(
                        StatefulExecutionTool.GetSnapshotId),
                    CreateEmptyArguments());
            }
            finally
            {
                executionState.Value = null;
            }
        }

        Task<object?> executionA =
            Task.Run(
                () =>
                    ExecuteAsync(
                        new ExecutionState(
                            "snapshot-A")));

        Task<object?> executionB =
            Task.Run(
                () =>
                    ExecuteAsync(
                        new ExecutionState(
                            "snapshot-B")));

        object?[] results =
            await Task.WhenAll(
                executionA,
                executionB);

        results
            .Should()
            .HaveCount(2);

        results[0]
            .Should()
            .Be("snapshot-A");

        results[1]
            .Should()
            .Be("snapshot-B");
    }

    [Fact]
    public async Task DispatchAsync_WhenExecutionLocalStateIsMissing_ShouldPropagateFactoryFailure()
    {
        AsyncLocal<ExecutionState?> executionState =
            new();

        GeminiToolRegistry registry =
            CreateRegistry();

        registry.Register<StatefulExecutionTool>(
            () =>
            {
                ExecutionState state =
                    executionState.Value
                    ?? throw new InvalidOperationException(
                        "No execution state is available.");

                return new StatefulExecutionTool(
                    state);
            });

        GeminiToolDispatcher dispatcher =
            CreateDispatcher(
                registry);

        Func<Task> act =
            async () =>
            {
                await dispatcher.DispatchAsync(
                    nameof(
                        StatefulExecutionTool.GetSnapshotId),
                    CreateEmptyArguments());
            };

        await act
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "*No execution state is available*");
    }

    [Fact]
    public async Task DispatchAsync_WhenExecutionCompletes_ShouldNotExposePreviousExecutionState()
    {
        AsyncLocal<ExecutionState?> executionState =
            new();

        GeminiToolRegistry registry =
            CreateRegistry();

        registry.Register<StatefulExecutionTool>(
            () =>
            {
                ExecutionState state =
                    executionState.Value
                    ?? throw new InvalidOperationException(
                        "No execution state is available.");

                return new StatefulExecutionTool(
                    state);
            });

        GeminiToolDispatcher dispatcher =
            CreateDispatcher(
                registry);

        executionState.Value =
            new ExecutionState(
                "snapshot-A");

        object? result;

        try
        {
            result =
                await dispatcher.DispatchAsync(
                    nameof(
                        StatefulExecutionTool.GetSnapshotId),
                    CreateEmptyArguments());
        }
        finally
        {
            executionState.Value = null;
        }

        result
            .Should()
            .Be("snapshot-A");

        Func<Task> act =
            async () =>
            {
                await dispatcher.DispatchAsync(
                    nameof(
                        StatefulExecutionTool.GetSnapshotId),
                    CreateEmptyArguments());
            };

        await act
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "*No execution state is available*");
    }

    private static GeminiToolRegistry CreateRegistry()
    {
        IExpressionTreeUtilityService
            expressionTreeUtilityService =
                new ExpressionTreeUtilityService();

        IReflectionUtilityService
            reflectionUtilityService =
                new ReflectionUtilityService(
                    expressionTreeUtilityService);

        return new GeminiToolRegistry(
            reflectionUtilityService);
    }

    private static GeminiToolDispatcher CreateDispatcher(
        GeminiToolRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(
            registry);

        ITypeUtilityService typeUtilityService =
            new TypeUtilityService();

        return new GeminiToolDispatcher(
            registry,
            typeUtilityService);
    }

    private static Dictionary<string, object>
        CreateEmptyArguments()
    {
        return new Dictionary<string, object>();
    }
}