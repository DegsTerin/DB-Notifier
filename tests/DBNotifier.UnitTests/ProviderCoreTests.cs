// Module purpose: Verifies Provider Core Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Monitoring;
using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.UnitTests;

public sealed class ProviderCoreTests
{
    [Theory]
    [InlineData("PostgreSQL", "postgresql")]
    [InlineData("sap-hana.custom", "sap-hana.custom")]
    [InlineData("future_engine.v2", "future_engine.v2")]
    public void ProviderTypeIsOpenAndCanonical(string input, string expected)
    {
        Assert.Equal(expected, ProviderType.Parse(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("provider with spaces")]
    [InlineData("-invalid")]
    [InlineData("provider/invalid")]
    public void ProviderTypeRejectsUnstableIdentifiers(string input)
    {
        Assert.False(ProviderType.TryParse(input, out _));
    }

    [Fact]
    public void ProviderEndpointRejectsSecretShapedProperties()
    {
        ProviderType providerType = ProviderType.Parse("custom-db");
        KeyValuePair<string, string>[] properties = [new("password", "must-not-enter-endpoint")];

        Assert.Throws<ArgumentException>(() => new ProviderEndpoint(providerType, properties));
    }

    [Fact]
    public void RegistryAcceptsAnArbitraryProviderWithoutCoreChanges()
    {
        StubProvider provider = new("future-db");
        ProviderRegistry registry = new([provider]);

        Assert.True(registry.TryResolve(ProviderType.Parse("future-db"), out IDatabaseProvider resolved));
        Assert.Same(provider, resolved);
    }

    [Fact]
    public void RegistryRejectsDuplicateProviderTypes()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ProviderRegistry([new StubProvider("duplicate"), new StubProvider("duplicate")]));
    }

    [Fact]
    public async Task UnknownProviderProducesUnknownObservationInsteadOfHealthy()
    {
        ProviderEndpoint endpoint = new(
            ProviderType.Parse("not-installed"),
            [new KeyValuePair<string, string>("host", "localhost")]);
        ProbeInstanceHandler handler = new(new ProviderRegistry([]), new UnusedVault(), TimeProvider.System);

        HealthObservation observation = await handler.ExecuteAsync(new ProbeInstanceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            endpoint,
            null,
            Policy()));

        Assert.Equal(HealthStatus.Unknown, observation.Status);
        Assert.Equal("provider.not_registered", observation.Error?.Code);
        Assert.Equal(EvidenceLevel.Unknown, observation.Quality.EvidenceLevel);
    }

    [Fact]
    public async Task ProviderFailureIsIsolatedAndDoesNotLeakExceptionDetails()
    {
        ThrowingProvider provider = new();
        ProbeInstanceHandler handler = new(new ProviderRegistry([provider]), new UnusedVault(), TimeProvider.System);
        ProviderEndpoint endpoint = new(provider.ProviderType, [new KeyValuePair<string, string>("host", "localhost")]);

        HealthObservation observation = await handler.ExecuteAsync(new ProbeInstanceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            endpoint,
            null,
            Policy()));

        Assert.Equal(HealthStatus.Unknown, observation.Status);
        Assert.Equal("provider.unhandled_failure", observation.Error?.Code);
        Assert.DoesNotContain("sensitive-provider-detail", observation.Error?.SafeMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetryPolicyUsesOneCredentialLeaseAndRecordsFinalAttempt()
    {
        RetryProvider provider = new();
        RecordingVault vault = new();
        ProbeInstanceHandler handler = new(new ProviderRegistry([provider]), vault, TimeProvider.System);
        CredentialReference reference = new(Guid.NewGuid(), "test-vault", "monitoring/db-1", CredentialPurpose.Monitoring);
        ProviderEndpoint endpoint = new(provider.ProviderType, [new KeyValuePair<string, string>("host", "localhost")]);

        HealthObservation observation = await handler.ExecuteAsync(new ProbeInstanceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            endpoint,
            reference,
            Policy(maxAttempts: 3)));

        Assert.Equal(HealthStatus.Healthy, observation.Status);
        Assert.Equal(3, observation.Quality.AttemptCount);
        Assert.Equal([1, 2, 3], provider.Attempts);
        Assert.Single(provider.Credentials.Distinct());
        Assert.Throws<ObjectDisposedException>(() => _ = vault.Lease!.Secret);
    }

    [Fact]
    public async Task MonitoringCycleIsolatesPersistenceFailurePerInstance()
    {
        StubProvider provider = new("cycle-provider");
        ProbeInstanceHandler handler = new(new ProviderRegistry([provider]), new UnusedVault(), TimeProvider.System);
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        ProviderEndpoint endpoint = new(provider.ProviderType, [new KeyValuePair<string, string>("host", "localhost")]);
        StaticAssignmentSource source = new([
            new MonitoringAssignment(first, endpoint, null, Policy()),
            new MonitoringAssignment(second, endpoint, null, Policy()),
        ]);
        SelectiveSink sink = new(first);
        MonitoringCycleRunner runner = new(Guid.NewGuid(), source, handler, sink, TimeProvider.System);

        MonitoringCycleResult result = await runner.RunOnceAsync();

        Assert.Equal(2, result.DueCount);
        Assert.Equal(1, result.PersistedCount);
        Assert.Collection(result.Failures, failure => Assert.Equal(first, failure.InstanceId));
        Assert.Contains(second, sink.PersistedInstances);
    }

    [Fact]
    public async Task MonitoringCycleLetsFastTargetCompleteWhileAnotherTargetWaits()
    {
        CoordinatedProvider provider = new();
        ProbeInstanceHandler handler = new(new ProviderRegistry([provider]), new UnusedVault(), TimeProvider.System);
        ProviderEndpoint slow = new(provider.ProviderType, [new KeyValuePair<string, string>("mode", "slow")]);
        ProviderEndpoint fast = new(provider.ProviderType, [new KeyValuePair<string, string>("mode", "fast")]);
        StaticAssignmentSource source = new([
            new MonitoringAssignment(Guid.NewGuid(), slow, null, Policy()),
            new MonitoringAssignment(Guid.NewGuid(), fast, null, Policy()),
        ]);
        SelectiveSink sink = new(Guid.Empty);
        MonitoringCycleRunner runner = new(
            Guid.NewGuid(),
            source,
            handler,
            sink,
            TimeProvider.System,
            maximumConcurrency: 2,
            cycleDeadline: TimeSpan.FromSeconds(2));

        MonitoringCycleResult result = await runner.RunOnceAsync();

        Assert.Equal(2, result.PersistedCount);
        Assert.Empty(result.Failures);
        Assert.True(provider.FastCompletedBeforeSlowReleased);
    }

    [Fact]
    public async Task MonitoringCycleDeadlinePersistsFastTargetAndMarksSlowTarget()
    {
        DeadlineProvider provider = new();
        ProbeInstanceHandler handler = new(new ProviderRegistry([provider]), new UnusedVault(), TimeProvider.System);
        Guid slowId = Guid.NewGuid();
        Guid fastId = Guid.NewGuid();
        ProviderEndpoint slow = new(provider.ProviderType, [new KeyValuePair<string, string>("mode", "slow")]);
        ProviderEndpoint fast = new(provider.ProviderType, [new KeyValuePair<string, string>("mode", "fast")]);
        StaticAssignmentSource source = new([
            new MonitoringAssignment(slowId, slow, null, Policy()),
            new MonitoringAssignment(fastId, fast, null, Policy()),
        ]);
        SelectiveSink sink = new(Guid.Empty);
        MonitoringCycleRunner runner = new(
            Guid.NewGuid(),
            source,
            handler,
            sink,
            TimeProvider.System,
            maximumConcurrency: 2,
            cycleDeadline: TimeSpan.FromMilliseconds(300));

        MonitoringCycleResult result = await runner.RunOnceAsync();

        Assert.Equal(2, result.DueCount);
        Assert.Equal(1, result.PersistedCount);
        Assert.Contains(fastId, sink.PersistedInstances);
        MonitoringCycleFailure failure = Assert.Single(result.Failures);
        Assert.Equal(slowId, failure.InstanceId);
        Assert.Equal("monitoring.cycle_deadline_exceeded", failure.Code);
    }

    [Fact]
    public async Task VaultFailureBecomesUnknownWithoutCallingProvider()
    {
        CountingProvider provider = new();
        ProbeInstanceHandler handler = new(new ProviderRegistry([provider]), new FailingVault(), TimeProvider.System);
        CredentialReference reference = new(Guid.NewGuid(), "failing-vault", "monitoring/db", CredentialPurpose.Monitoring);
        ProviderEndpoint endpoint = new(provider.ProviderType, [new KeyValuePair<string, string>("host", "localhost")]);

        HealthObservation observation = await handler.ExecuteAsync(new ProbeInstanceCommand(
            Guid.NewGuid(), Guid.NewGuid(), endpoint, reference, Policy()));

        Assert.Equal(HealthStatus.Unknown, observation.Status);
        Assert.Equal("credential.unavailable", observation.Error?.Code);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task AdministrativeCredentialCannotBeUsedForMonitoring()
    {
        StubProvider provider = new("purpose-provider");
        ProbeInstanceHandler handler = new(new ProviderRegistry([provider]), new UnusedVault(), TimeProvider.System);
        CredentialReference reference = new(Guid.NewGuid(), "vault", "admin/db", CredentialPurpose.Administration);
        ProviderEndpoint endpoint = new(provider.ProviderType, [new KeyValuePair<string, string>("host", "localhost")]);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await handler.ExecuteAsync(new ProbeInstanceCommand(
                Guid.NewGuid(), Guid.NewGuid(), endpoint, reference, Policy())));
    }

    [Fact]
    public async Task ExpiredMonitoringCredentialFailsBeforeProviderCall()
    {
        DateTimeOffset now = new(2026, 7, 12, 21, 0, 0, TimeSpan.Zero);
        CountingProvider provider = new();
        ProbeInstanceHandler handler = new(
            new ProviderRegistry([provider]),
            new ExpiredVault(now.AddSeconds(-1)),
            new FixedTimeProvider(now));
        CredentialReference reference = new(Guid.NewGuid(), "test-vault", "monitoring/expired", CredentialPurpose.Monitoring);
        ProviderEndpoint endpoint = new(provider.ProviderType, [new KeyValuePair<string, string>("host", "localhost")]);

        HealthObservation observation = await handler.ExecuteAsync(new ProbeInstanceCommand(
            Guid.NewGuid(), Guid.NewGuid(), endpoint, reference, Policy()));

        Assert.Equal(HealthStatus.AuthFailed, observation.Status);
        Assert.Equal("credential.expired", observation.Error?.Code);
        Assert.Equal(0, provider.CallCount);
    }

    private static ProbePolicy Policy(int maxAttempts = 1) =>
        new(TimeSpan.FromSeconds(1), maxAttempts, TimeSpan.Zero, TimeSpan.Zero);

    private sealed class StubProvider(string providerType) : IDatabaseProvider
    {
        public ProviderType ProviderType { get; } = ProviderType.Parse(providerType);

        public string Version => "test";

        public IReadOnlyList<ProviderCapability> Capabilities => [];

        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint) => ProviderValidationResult.Valid;

        public ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new ProviderProbeResult(
                HealthStatus.Healthy,
                EvidenceLevel.ProviderAuthenticated,
                "test",
                TimeSpan.Zero,
                null,
                []));
    }

    private sealed class ThrowingProvider : IDatabaseProvider
    {
        public ProviderType ProviderType { get; } = ProviderType.Parse("throwing-provider");

        public string Version => "test";

        public IReadOnlyList<ProviderCapability> Capabilities => [];

        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint) => ProviderValidationResult.Valid;

        public ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("sensitive-provider-detail");
    }

    private sealed class UnusedVault : ICredentialVault
    {
        public ValueTask<IProviderCredential> ResolveAsync(
            CredentialReference reference,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Vault should not be called without a credential reference.");
    }

    private sealed class RecordingVault : ICredentialVault
    {
        public ProviderCredentialLease? Lease { get; private set; }

        public ValueTask<IProviderCredential> ResolveAsync(
            CredentialReference reference,
            CancellationToken cancellationToken)
        {
            Lease = new ProviderCredentialLease("monitor", "temporary-password".AsSpan());
            return ValueTask.FromResult<IProviderCredential>(Lease);
        }
    }

    private sealed class RetryProvider : IDatabaseProvider
    {
        public ProviderType ProviderType { get; } = ProviderType.Parse("retry-provider");

        public string Version => "test";

        public IReadOnlyList<ProviderCapability> Capabilities => [];

        public List<int> Attempts { get; } = [];

        public List<IProviderCredential?> Credentials { get; } = [];

        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint) => ProviderValidationResult.Valid;

        public ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken)
        {
            Attempts.Add(request.AttemptNumber);
            Credentials.Add(request.MonitoringCredential);
            bool healthy = request.AttemptNumber == 3;
            return ValueTask.FromResult(new ProviderProbeResult(
                healthy ? HealthStatus.Healthy : HealthStatus.Unavailable,
                EvidenceLevel.ProviderAuthenticated,
                "retry-fixture",
                TimeSpan.Zero,
                healthy
                    ? null
                    : new NormalizedError("fixture.retry", ErrorCategory.Network, Retryability.Backoff, "Retry fixture."),
                []));
        }
    }

    private sealed class StaticAssignmentSource(IReadOnlyList<MonitoringAssignment> assignments) : IMonitoringAssignmentSource
    {
        public ValueTask<IReadOnlyList<MonitoringAssignment>> GetDueAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken) => ValueTask.FromResult(assignments);
    }

    private sealed class SelectiveSink(Guid failingInstanceId) : IHealthObservationSink
    {
        public List<Guid> PersistedInstances { get; } = [];

        public ValueTask PersistAsync(HealthObservation observation, CancellationToken cancellationToken)
        {
            if (observation.InstanceId == failingInstanceId)
            {
                throw new InvalidOperationException("Expected persistence failure.");
            }

            PersistedInstances.Add(observation.InstanceId);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingVault : ICredentialVault
    {
        public ValueTask<IProviderCredential> ResolveAsync(
            CredentialReference reference,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("sensitive-vault-detail");
    }

    private sealed class ExpiredVault(DateTimeOffset expiresAt) : ICredentialVault
    {
        public ValueTask<IProviderCredential> ResolveAsync(
            CredentialReference reference,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IProviderCredential>(
                new ProviderCredentialLease("monitor", "expired-fixture".AsSpan(), expiresAt));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class CountingProvider : IDatabaseProvider
    {
        public ProviderType ProviderType { get; } = ProviderType.Parse("counting-provider");

        public string Version => "test";

        public IReadOnlyList<ProviderCapability> Capabilities => [];

        public int CallCount { get; private set; }

        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint) => ProviderValidationResult.Valid;

        public ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(new ProviderProbeResult(
                HealthStatus.Healthy, EvidenceLevel.ProviderAuthenticated, "fixture", TimeSpan.Zero, null, []));
        }
    }

    private sealed class CoordinatedProvider : IDatabaseProvider
    {
        private readonly TaskCompletionSource fastCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ProviderType ProviderType { get; } = ProviderType.Parse("coordinated-provider");

        public string Version => "test";

        public IReadOnlyList<ProviderCapability> Capabilities => [];

        public bool FastCompletedBeforeSlowReleased { get; private set; }

        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint) => ProviderValidationResult.Valid;

        public async ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Endpoint.TryGetValue("mode", out string mode) && mode == "slow")
            {
                await fastCompleted.Task.WaitAsync(cancellationToken);
                FastCompletedBeforeSlowReleased = true;
            }
            else
            {
                fastCompleted.TrySetResult();
            }

            return new ProviderProbeResult(
                HealthStatus.Healthy,
                EvidenceLevel.ProviderAuthenticated,
                "fixture",
                TimeSpan.Zero,
                null,
                []);
        }
    }

    private sealed class DeadlineProvider : IDatabaseProvider
    {
        public ProviderType ProviderType { get; } = ProviderType.Parse("deadline-provider");

        public string Version => "test";

        public IReadOnlyList<ProviderCapability> Capabilities => [];

        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint) => ProviderValidationResult.Valid;

        public async ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Endpoint.TryGetValue("mode", out string mode) && mode == "slow")
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new ProviderProbeResult(
                HealthStatus.Healthy,
                EvidenceLevel.ProviderAuthenticated,
                "fixture",
                TimeSpan.Zero,
                null,
                []);
        }
    }
}
