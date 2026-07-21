// Module purpose: Supplies the exact test-only provider schema used by Agent Fleet loopback integration fixtures.
using DBNotifier.Application.AgentFleet;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.IntegrationTests;

/// <summary>Creates a non-executable provider registry and monitoring-reference document for integration tests.</summary>
internal static class AgentAssignmentValidationFixture
{
    /// <summary>Gets one deterministic monitoring-purpose opaque reference containing no resolved credential.</summary>
    internal const string MonitoringReference =
        "{\"referenceId\":\"22222222-2222-4222-8222-222222222222\",\"vaultProvider\":\"fixture-vault\",\"locator\":\"monitoring/integration\",\"purpose\":\"Monitoring\"}";

    /// <summary>Creates the exact validator used on both sides of the local loopback assignment boundary.</summary>
    /// <returns>A validator for the single <c>fixture-provider</c> schema.</returns>
    internal static IAgentAssignmentValidator Create() =>
        new AgentAssignmentValidator(new ProviderRegistry([new FixtureProvider()]));

    /// <summary>Defines a two-field endpoint schema and deliberately provides no probe capability.</summary>
    private sealed class FixtureProvider : IDatabaseProvider
    {
        /// <inheritdoc />
        public ProviderType ProviderType { get; } = ProviderType.Parse("fixture-provider");

        /// <inheritdoc />
        public string Version => "test-only";

        /// <inheritdoc />
        public IReadOnlyList<ProviderCapability> Capabilities => [];

        /// <inheritdoc />
        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint)
        {
            string[] keys = endpoint.Properties.Keys.Order(StringComparer.Ordinal).ToArray();
            return endpoint.ProviderType == ProviderType &&
                keys.SequenceEqual(["host", "port"], StringComparer.Ordinal) &&
                endpoint.TryGetValue("host", out string host) && !string.IsNullOrWhiteSpace(host) &&
                endpoint.TryGetValue("port", out string port) && int.TryParse(port, out int parsedPort) &&
                parsedPort is >= 1 and <= 65535
                    ? ProviderValidationResult.Valid
                    : ProviderValidationResult.Invalid(
                        new ProviderValidationError("fixture.endpoint_invalid", "endpoint", "The fixture endpoint is invalid."));
        }

        /// <inheritdoc />
        public ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("The integration assignment fixture cannot probe a database.");
    }
}
