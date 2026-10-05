using AiUtility.ToolKits.Execution;
using FluentAssertions;

namespace AiUtility.ToolKits.Tests.Execution;

public sealed class AiToolExecutionStateAccessorTests
{
    [Fact]
    public void Current_WhenNoExecutionStateExists_ShouldThrow()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        Action act =
            () => _ = sut.Current;

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*No active AI tool execution state*");
    }

    [Fact]
    public void Push_WhenStateIsNull_ShouldThrow()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        Action act =
            () => sut.Push(null!);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithMessage("*state*");
    }

    [Fact]
    public void Push_WhenStateIsActive_ShouldExposeExactStateInstance()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var expected =
            new ExecutionState(
                "execution-A");

        using IDisposable lease =
            sut.Push(expected);

        ExecutionState actual =
            sut.Current;

        actual.Should()
            .BeSameAs(expected);
    }

    [Fact]
    public void Dispose_WhenExecutionCompletes_ShouldRemoveExecutionState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var state =
            new ExecutionState(
                "execution-A");

        IDisposable lease =
            sut.Push(state);

        lease.Dispose();

        Action act =
            () => _ = sut.Current;

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*No active AI tool execution state*");
    }

    [Fact]
    public void Dispose_WhenNestedExecutionCompletes_ShouldRestorePreviousState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var outerState =
            new ExecutionState(
                "execution-A");

        var innerState =
            new ExecutionState(
                "execution-B");

        using IDisposable outerLease =
            sut.Push(outerState);

        sut.Current.Should()
            .BeSameAs(outerState);

        using (IDisposable innerLease =
               sut.Push(innerState))
        {
            sut.Current.Should()
                .BeSameAs(innerState);
        }

        sut.Current.Should()
            .BeSameAs(outerState);
    }

    [Fact]
    public void Dispose_WhenOuterExecutionCompletesAfterNestedExecution_ShouldRemoveAllState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var outerState =
            new ExecutionState(
                "execution-A");

        var innerState =
            new ExecutionState(
                "execution-B");

        using (IDisposable outerLease =
               sut.Push(outerState))
        {
            using (IDisposable innerLease =
                   sut.Push(innerState))
            {
                sut.Current.Should()
                    .BeSameAs(innerState);
            }

            sut.Current.Should()
                .BeSameAs(outerState);
        }

        Action act =
            () => _ = sut.Current;

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*No active AI tool execution state*");
    }

    [Fact]
    public void Dispose_WhenCalledTwice_ShouldRemainSafeAndNotCorruptState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var state =
            new ExecutionState(
                "execution-A");

        IDisposable lease =
            sut.Push(state);

        lease.Dispose();

        Action act =
            () => lease.Dispose();

        act.Should()
            .NotThrow();

        Action readCurrent =
            () => _ = sut.Current;

        readCurrent.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*No active AI tool execution state*");
    }

    [Fact]
    public void Dispose_WhenInnerLeaseIsDisposedTwice_ShouldNotRemoveOuterState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var outerState =
            new ExecutionState(
                "execution-A");

        var innerState =
            new ExecutionState(
                "execution-B");

        using IDisposable outerLease =
            sut.Push(outerState);

        IDisposable innerLease =
            sut.Push(innerState);

        innerLease.Dispose();
        innerLease.Dispose();

        sut.Current.Should()
            .BeSameAs(outerState);
    }

    [Fact]
    public void Dispose_WhenExecutionThrows_ShouldRestorePreviousState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var outerState =
            new ExecutionState(
                "execution-A");

        var innerState =
            new ExecutionState(
                "execution-B");

        using IDisposable outerLease =
            sut.Push(outerState);

        Action act =
            () =>
            {
                using IDisposable innerLease =
                    sut.Push(innerState);

                throw new InvalidOperationException(
                    "Simulated tool failure.");
            };

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*Simulated tool failure*");

        sut.Current.Should()
            .BeSameAs(outerState);
    }

    [Fact]
    public async Task Push_WhenExecutionsRunConcurrently_ShouldPreserveExecutionAffinity()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var stateA =
            new ExecutionState(
                "execution-A");

        var stateB =
            new ExecutionState(
                "execution-B");

        var bothReady =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var readyCount = 0;

        async Task<ExecutionState> ExecuteAsync(
            ExecutionState state)
        {
            using IDisposable lease =
                sut.Push(state);

            if (Interlocked.Increment(
                    ref readyCount) == 2)
            {
                bothReady.SetResult(true);
            }

            await bothReady.Task;

            await Task.Yield();

            return sut.Current;
        }

        Task<ExecutionState> executionA =
            Task.Run(
                () => ExecuteAsync(stateA));

        Task<ExecutionState> executionB =
            Task.Run(
                () => ExecuteAsync(stateB));

        ExecutionState[] results =
            await Task.WhenAll(
                executionA,
                executionB);

        results.Should()
            .HaveCount(2);

        results[0].Should()
            .BeSameAs(stateA);

        results[1].Should()
            .BeSameAs(stateB);
    }

    [Fact]
    public async Task Push_WhenExecutionCrossesAwaitBoundary_ShouldPreserveState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var expected =
            new ExecutionState(
                "execution-A");

        using IDisposable lease =
            sut.Push(expected);

        await Task.Yield();

        ExecutionState actual =
            sut.Current;

        actual.Should()
            .BeSameAs(expected);
    }

    [Fact]
    public async Task Dispose_WhenAsyncExecutionCompletes_ShouldNotLeakState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var state =
            new ExecutionState(
                "execution-A");

        async Task ExecuteAsync()
        {
            using IDisposable lease =
                sut.Push(state);

            await Task.Yield();

            sut.Current.Should()
                .BeSameAs(state);
        }

        await ExecuteAsync();

        Action act =
            () => _ = sut.Current;

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*No active AI tool execution state*");
    }

    [Fact]
    public async Task Dispose_WhenAsyncExecutionIsCancelled_ShouldNotLeakState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var state =
            new ExecutionState(
                "execution-A");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        async Task ExecuteAsync()
        {
            using IDisposable lease =
                sut.Push(state);

            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationTokenSource.Token);
        }

        Func<Task> act =
            ExecuteAsync;

        await act.Should()
            .ThrowAsync<OperationCanceledException>()
            .WithMessage("*");

        Action readCurrent =
            () => _ = sut.Current;

        readCurrent.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*No active AI tool execution state*");
    }

    [Fact]
    public void Dispose_WhenLeaseIsDisposedOutOfOrder_ShouldThrowAndPreserveCurrentState()
    {
        var sut =
            new AiToolExecutionStateAccessor<ExecutionState>();

        var outerState =
            new ExecutionState(
                "execution-A");

        var innerState =
            new ExecutionState(
                "execution-B");

        IDisposable outerLease =
            sut.Push(outerState);

        IDisposable innerLease =
            sut.Push(innerState);

        Action act =
            () => outerLease.Dispose();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*must be disposed in reverse order*");

        sut.Current.Should()
            .BeSameAs(innerState);

        innerLease.Dispose();
        outerLease.Dispose();

        Action readCurrent =
            () => _ = sut.Current;

        readCurrent.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*No active AI tool execution state*");
    }

    private sealed record ExecutionState(
        string ExecutionId);
}