using DBNotifier.Application.Monitoring;
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
        ProbeInstanceHandler handler = new(new ProviderRegistry([]), TimeProvider.System);

        HealthObservation observation = await handler.ExecuteAsync(new ProbeInstanceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            endpoint,
            null,
            TimeSpan.FromSeconds(1)));

        Assert.Equal(HealthStatus.Unknown, observation.Status);
        Assert.Equal("provider.not_registered", observation.Error?.Code);
        Assert.Equal(EvidenceLevel.Unknown, observation.Quality.EvidenceLevel);
    }

    [Fact]
    public async Task ProviderFailureIsIsolatedAndDoesNotLeakExceptionDetails()
    {
        ThrowingProvider provider = new();
        ProbeInstanceHandler handler = new(new ProviderRegistry([provider]), TimeProvider.System);
        ProviderEndpoint endpoint = new(provider.ProviderType, [new KeyValuePair<string, string>("host", "localhost")]);

        HealthObservation observation = await handler.ExecuteAsync(new ProbeInstanceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            endpoint,
            null,
            TimeSpan.FromSeconds(1)));

        Assert.Equal(HealthStatus.Unknown, observation.Status);
        Assert.Equal("provider.unhandled_failure", observation.Error?.Code);
        Assert.DoesNotContain("sensitive-provider-detail", observation.Error?.SafeMessage, StringComparison.Ordinal);
    }

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
}
