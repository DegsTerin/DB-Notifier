// Module purpose: Supplies a provider-neutral assignment schema fixture for local unit tests without activating a provider runtime.
using DBNotifier.Application.AgentFleet;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.UnitTests;

/// <summary>Creates exact non-secret assignment-validation fixtures shared by Agent Fleet unit tests.</summary>
internal static class AgentAssignmentValidationFixture
{
    /// <summary>Gets one deterministic monitoring-only opaque-reference document.</summary>
    internal const string MonitoringReference =
        "{\"referenceId\":\"11111111-1111-4111-8111-111111111111\",\"vaultProvider\":\"fixture-vault\",\"locator\":\"monitoring/fixture\",\"purpose\":\"Monitoring\"}";

    /// <summary>Creates a validator containing only the exact provider identifier requested by the test.</summary>
    /// <param name="providerType">Canonical fixture provider identifier.</param>
    /// <returns>A provider-backed assignment validator that accepts only <c>host</c> and <c>port</c>.</returns>
    internal static IAgentAssignmentValidator Create(string providerType = "postgresql") =>
        new AgentAssignmentValidator(new ProviderRegistry([new FixtureProvider(providerType)]));

    /// <summary>Defines only endpoint-schema validation; probing remains unavailable in this local fixture.</summary>
    private sealed class FixtureProvider(string providerType) : IDatabaseProvider
    {
        private static readonly IReadOnlyList<ProviderCapability> NoCapabilities = [];

        /// <inheritdoc />
        public ProviderType ProviderType { get; } = ProviderType.Parse(providerType);

        /// <inheritdoc />
        public string Version => "test-only";

        /// <inheritdoc />
        public IReadOnlyList<ProviderCapability> Capabilities => NoCapabilities;

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
            throw new NotSupportedException("The assignment schema fixture cannot probe a database.");
    }
}
