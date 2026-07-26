// Module purpose: Verifies the cancellation, release and Agent-isolation lifecycle of the internal identity transaction fence.
using DBNotifier.Persistence.Server.PostgreSql;

namespace DBNotifier.UnitTests;

/// <summary>
/// Exercises the internal per-Agent identity gate directly so lifecycle regressions cannot split an owned gate,
/// leak a failed owner or serialise unrelated Agents.
/// </summary>
public sealed class AgentIdentityTransactionFenceTests
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Proves a cancelled waiter cannot remove an owned entry or let a later waiter acquire a split gate.</summary>
    [Fact]
    public async Task CancelledWaiterCannotSplitOwnedGateForLaterWaiter()
    {
        Guid agentId = Guid.NewGuid();
        using IDisposable owner = await AgentIdentityTransactionFence
            .EnterAsync(agentId, CancellationToken.None);
        using CancellationTokenSource cancellation = new();

        Task<IDisposable> cancelledWaiter = AgentIdentityTransactionFence
            .EnterAsync(agentId, cancellation.Token)
            .AsTask();
        Assert.False(cancelledWaiter.IsCompleted);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWaiter);

        Task<IDisposable> laterWaiter = AgentIdentityTransactionFence
            .EnterAsync(agentId, CancellationToken.None)
            .AsTask();
        bool acquiredBeforeOwnerReleased = laterWaiter.IsCompleted;

        owner.Dispose();
        using IDisposable laterLease = await laterWaiter.WaitAsync(CompletionTimeout);

        Assert.False(acquiredBeforeOwnerReleased);
    }

    /// <summary>Proves exception unwinding releases the exact owned gate for the next operation.</summary>
    [Fact]
    public async Task ExceptionUnwindingReleasesOwnedGate()
    {
        Guid agentId = Guid.NewGuid();

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => EnterAndFailAsync(agentId));
        Assert.Equal("synthetic.identity_fence_failure", failure.Message);

        using IDisposable successor = await AgentIdentityTransactionFence
            .EnterAsync(agentId, CancellationToken.None)
            .AsTask()
            .WaitAsync(CompletionTimeout);
    }

    /// <summary>Proves repeated disposal is harmless and does not add an extra semaphore permit.</summary>
    [Fact]
    public async Task RepeatedDisposeReleasesLeaseOnlyOnce()
    {
        Guid agentId = Guid.NewGuid();
        IDisposable owner = await AgentIdentityTransactionFence
            .EnterAsync(agentId, CancellationToken.None);

        owner.Dispose();
        owner.Dispose();

        using IDisposable successor = await AgentIdentityTransactionFence
            .EnterAsync(agentId, CancellationToken.None)
            .AsTask()
            .WaitAsync(CompletionTimeout);
        Task<IDisposable> queued = AgentIdentityTransactionFence
            .EnterAsync(agentId, CancellationToken.None)
            .AsTask();
        bool acquiredBeforeSuccessorReleased = queued.IsCompleted;

        successor.Dispose();
        using IDisposable queuedLease = await queued.WaitAsync(CompletionTimeout);

        Assert.False(acquiredBeforeSuccessorReleased);
    }

    /// <summary>Proves one Agent's occupied gate does not serialise a different Agent.</summary>
    [Fact]
    public async Task DistinctAgentsAcquireIndependentGates()
    {
        Guid firstAgentId = Guid.NewGuid();
        Guid secondAgentId = Guid.NewGuid();
        using IDisposable firstOwner = await AgentIdentityTransactionFence
            .EnterAsync(firstAgentId, CancellationToken.None);
        Task<IDisposable> firstAgentWaiter = AgentIdentityTransactionFence
            .EnterAsync(firstAgentId, CancellationToken.None)
            .AsTask();

        using IDisposable secondAgentLease = await AgentIdentityTransactionFence
            .EnterAsync(secondAgentId, CancellationToken.None)
            .AsTask()
            .WaitAsync(CompletionTimeout);
        bool firstAgentAcquiredBeforeRelease = firstAgentWaiter.IsCompleted;

        firstOwner.Dispose();
        using IDisposable firstAgentLease = await firstAgentWaiter.WaitAsync(CompletionTimeout);

        Assert.False(firstAgentAcquiredBeforeRelease);
    }

    /// <summary>Acquires one gate and throws so language-level disposal must release the lease.</summary>
    /// <param name="agentId">Agent whose gate must be released during exception unwinding.</param>
    /// <returns>A task that always fails after acquiring the gate.</returns>
    /// <exception cref="InvalidOperationException">Always thrown to exercise the exceptional release path.</exception>
    private static async Task EnterAndFailAsync(Guid agentId)
    {
        using IDisposable owner = await AgentIdentityTransactionFence
            .EnterAsync(agentId, CancellationToken.None);
        throw new InvalidOperationException("synthetic.identity_fence_failure");
    }
}
