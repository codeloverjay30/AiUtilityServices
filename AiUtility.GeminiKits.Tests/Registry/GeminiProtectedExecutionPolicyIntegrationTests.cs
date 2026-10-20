using AiUtility.GeminiKits.Executor;
using AiUtility.GeminiKits.Registry;
using AiUtility.GeminiKits.Security;
using AiUtility.GeminiKits.Services;
using ExpressionTreeUtilityServices;
using FluentAssertions;
using ReflectionUtilityServices;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Tests.Registry;

public sealed class GeminiProtectedExecutionPolicyIntegrationTests
{
    [Theory]
    [InlineData("ClickAbsolute")]
    [InlineData("clickabsolute")]
    [InlineData("CLICKABSOLUTE")]
    public async Task DispatchAsync_WhenDirectClickIsRegisteredButNotAllowed_ShouldRejectBeforeClick(string name)
    {
        var device = new GeminiDispatcherSecurityIntegrationTests.DeviceClickProbe();
        var registry = CreateRegistry();
        registry.Register<GeminiDispatcherSecurityIntegrationTests.DirectClickTool>(
            () => new GeminiDispatcherSecurityIntegrationTests.DirectClickTool(device));
        var dispatcher = new GeminiToolDispatcher(registry, new TypeUtilityService(), PlannerOnlyPolicy());

        Func<Task> act = async () => await dispatcher.DispatchAsync(
            name, new Dictionary<string, object> { ["x"] = 10, ["y"] = 20 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Protected policy*ClickAbsolute*");
        device.ClickCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDirectClickIsRegisteredButNotAllowed_ShouldRejectBeforeClick()
    {
        var device = new GeminiDispatcherSecurityIntegrationTests.DeviceClickProbe();
        var registry = CreateRegistry();
        registry.Register<GeminiDispatcherSecurityIntegrationTests.DirectClickTool>(
            () => new GeminiDispatcherSecurityIntegrationTests.DirectClickTool(device));
        var executor = new GeminiToolExecutor(registry, new TypeUtilityService(), PlannerOnlyPolicy());

        Func<Task> act = async () => await executor.ExecuteAsync(
            "ClickAbsolute", new Dictionary<string, object> { ["x"] = 10, ["y"] = 20 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Protected policy*ClickAbsolute*");
        device.ClickCount.Should().Be(0);
    }

    [Fact]
    public async Task DispatchAsync_WhenPlannerMethodIsAllowed_ShouldExecute()
    {
        var device = new GeminiDispatcherSecurityIntegrationTests.DeviceClickProbe();
        var registry = CreateRegistry();
        registry.Register<GeminiDispatcherSecurityIntegrationTests.SafePlannerTool>(
            () => new GeminiDispatcherSecurityIntegrationTests.SafePlannerTool(device));
        var dispatcher = new GeminiToolDispatcher(registry, new TypeUtilityService(), PlannerOnlyPolicy());

        object? result = await dispatcher.DispatchAsync("PlannerClick", new Dictionary<string, object>());

        result.Should().Be("planner");
        device.ClickCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlannerMethodIsAllowed_ShouldExecute()
    {
        var device = new GeminiDispatcherSecurityIntegrationTests.DeviceClickProbe();
        var registry = CreateRegistry();
        registry.Register<GeminiDispatcherSecurityIntegrationTests.SafePlannerTool>(
            () => new GeminiDispatcherSecurityIntegrationTests.SafePlannerTool(device));
        var executor = new GeminiToolExecutor(registry, new TypeUtilityService(), PlannerOnlyPolicy());

        object? result = await executor.ExecuteAsync("PlannerClick", new Dictionary<string, object>());

        result.Should().Be("planner");
        device.ClickCount.Should().Be(0);
    }

    [Fact]
    public async Task DispatchAsync_WhenPolicyIsEmpty_ShouldDenyRegisteredPlanner()
    {
        var device = new GeminiDispatcherSecurityIntegrationTests.DeviceClickProbe();
        var registry = CreateRegistry();
        registry.Register<GeminiDispatcherSecurityIntegrationTests.SafePlannerTool>(
            () => new GeminiDispatcherSecurityIntegrationTests.SafePlannerTool(device));
        var dispatcher = new GeminiToolDispatcher(
            registry, new TypeUtilityService(), GeminiToolExecutionPolicy.Protected());

        Func<Task> act = async () => await dispatcher.DispatchAsync(
            "PlannerClick", new Dictionary<string, object>());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Protected policy*PlannerClick*");
        device.ClickCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPolicyIsEmpty_ShouldDenyRegisteredPlanner()
    {
        var device = new GeminiDispatcherSecurityIntegrationTests.DeviceClickProbe();
        var registry = CreateRegistry();
        registry.Register<GeminiDispatcherSecurityIntegrationTests.SafePlannerTool>(
            () => new GeminiDispatcherSecurityIntegrationTests.SafePlannerTool(device));
        var executor = new GeminiToolExecutor(
            registry, new TypeUtilityService(), GeminiToolExecutionPolicy.Protected());

        Func<Task> act = async () => await executor.ExecuteAsync(
            "PlannerClick", new Dictionary<string, object>());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Protected policy*PlannerClick*");
        device.ClickCount.Should().Be(0);
    }

    private static GeminiToolExecutionPolicy PlannerOnlyPolicy()
    {
        var method = typeof(GeminiDispatcherSecurityIntegrationTests.SafePlannerTool)
            .GetMethod(nameof(GeminiDispatcherSecurityIntegrationTests.SafePlannerTool.PlannerClick))!;
        return GeminiToolExecutionPolicy.Protected(method);
    }

    private static GeminiToolRegistry CreateRegistry()
    {
        IExpressionTreeUtilityService expressions = new ExpressionTreeUtilityService();
        IReflectionUtilityService reflection = new ReflectionUtilityService(expressions);
        return new GeminiToolRegistry(reflection);
    }
}
