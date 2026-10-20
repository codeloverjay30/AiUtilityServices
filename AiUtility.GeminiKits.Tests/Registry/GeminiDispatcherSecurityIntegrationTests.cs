using AiUtility.GeminiKits.Attributes;
using AiUtility.GeminiKits.Registry;
using AiUtility.GeminiKits.Services;
using ExpressionTreeUtilityServices;
using FluentAssertions;
using ReflectionUtilityServices;
using TypeUtilityServices;

namespace AiUtility.GeminiKits.Tests.Registry;

public sealed class GeminiDispatcherSecurityIntegrationTests
{
    [Theory]
    [InlineData("ClickAbsolute")]
    [InlineData("clickabsolute")]
    [InlineData("CLICKABSOLUTE")]
    public async Task DispatchAsync_WhenDirectClickIsNotRegistered_ShouldRejectWithoutDeviceSideEffects(string functionName)
    {
        var device = new DeviceClickProbe();
        var registry = CreateRegistry();
        var dispatcher = new GeminiToolDispatcher(registry, new TypeUtilityService());

        Func<Task> act = async () => await dispatcher.DispatchAsync(
            functionName,
            new Dictionary<string, object> { ["x"] = 10, ["y"] = 20 });

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{functionName}*");
        device.ClickCount.Should().Be(0);
    }

    [Fact]
    public async Task DispatchAsync_WhenUnannotatedMethodNameIsProvided_ShouldRejectWithoutInvokingIt()
    {
        var device = new DeviceClickProbe();
        var registry = CreateRegistry();
        registry.Register<SafePlannerTool>(() => new SafePlannerTool(device));
        var dispatcher = new GeminiToolDispatcher(registry, new TypeUtilityService());

        Func<Task> act = async () => await dispatcher.DispatchAsync(
            nameof(SafePlannerTool.UnannotatedDirectClick),
            new Dictionary<string, object>());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*UnannotatedDirectClick*");
        device.ClickCount.Should().Be(0);
    }

    [Fact]
    public async Task DispatchAsync_WhenDirectClickIsExplicitlyRegistered_ShouldExecute_ExposingMissingRuntimePolicy()
    {
        var device = new DeviceClickProbe();
        var registry = CreateRegistry();
        registry.Register<DirectClickTool>(() => new DirectClickTool(device));
        var dispatcher = new GeminiToolDispatcher(registry, new TypeUtilityService());

        object? result = await dispatcher.DispatchAsync(
            nameof(DirectClickTool.ClickAbsolute),
            new Dictionary<string, object> { ["x"] = 10, ["y"] = 20 });

        result.Should().Be("clicked");
        device.ClickCount.Should().Be(1);
    }

    private static GeminiToolRegistry CreateRegistry()
    {
        IExpressionTreeUtilityService expressions = new ExpressionTreeUtilityService();
        IReflectionUtilityService reflection = new ReflectionUtilityService(expressions);
        return new GeminiToolRegistry(reflection);
    }

    /// <summary>Records simulated device clicks without accessing a real device.</summary>
    public sealed class DeviceClickProbe
    {
        public int ClickCount { get; private set; }

        /// <summary>Records one simulated device click.</summary>
        public void Click() => ClickCount++;
    }

    /// <summary>Exposes one approved tool and one unannotated method.</summary>
    public sealed class SafePlannerTool
    {
        private readonly DeviceClickProbe _device;

        /// <summary>Initializes the tool with a click probe.</summary>
        public SafePlannerTool(DeviceClickProbe device) => _device = device;

        [GeminiTool]
        /// <summary>Returns a simulated planner result.</summary>
        public string PlannerClick() => "planner";

        /// <summary>Simulates an unannotated direct click.</summary>
        public string UnannotatedDirectClick()
        {
            _device.Click();
            return "clicked";
        }
    }

    /// <summary>Simulates a directly registered click tool.</summary>
    public sealed class DirectClickTool
    {
        private readonly DeviceClickProbe _device;

        /// <summary>Initializes the tool with a click probe.</summary>
        public DirectClickTool(DeviceClickProbe device) => _device = device;

        [GeminiTool]
        /// <summary>Records a simulated absolute click.</summary>
        public string ClickAbsolute(int x, int y)
        {
            _device.Click();
            return "clicked";
        }
    }
}
