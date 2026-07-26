// Module purpose: Guards normal host composition against bypassing the shared network-egress and offline TLS boundaries.
namespace DBNotifier.Architecture.Tests;

/// <summary>
/// Protects the Agent, Server, provider and compatibility composition points that own potentially external traffic.
/// </summary>
public sealed class NetworkEgressCompositionTests
{
    /// <summary>Confirms normal Agent composition uses policy-bound HTTP and provider services while dormant transports stay absent.</summary>
    [Fact]
    public void NormalAgentCompositionUsesTheSharedNetworkEgressBoundary()
    {
        string program = Read("src", "DBNotifier.Agent.Worker", "Program.cs");

        Assert.Contains("NetworkEgressPolicySet.Compile", program, StringComparison.Ordinal);
        Assert.Contains("NetworkBoundHttpMessageHandlerFactory", program, StringComparison.Ordinal);
        Assert.Contains("NetworkEgressPolicyIds.AgentSynchronization", program, StringComparison.Ordinal);
        Assert.Contains("NetworkEgressPolicyIds.ProviderMonitoring", program, StringComparison.Ordinal);
        Assert.Contains(
            "CreateSynchronizationHandler(synchronizationOptions, networkHttpFactory)",
            program,
            StringComparison.Ordinal);
        Assert.DoesNotContain("new HttpClientHandler", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpCommandDeliveryTransport", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpCommandTransportSandboxClient", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpAgentFleetClientTransport", program, StringComparison.Ordinal);
    }

    /// <summary>Confirms normal Server composition cannot fall back to ambient OIDC, certificate or PostgreSQL networking.</summary>
    [Fact]
    public void NormalServerCompositionUsesPinnedOidcTlsAndDatabaseBoundaries()
    {
        string program = Read("src", "DBNotifier.Server.Api", "Program.cs");

        Assert.Contains("NetworkEgressPolicySet.Compile", program, StringComparison.Ordinal);
        Assert.Contains("NetworkBoundHttpMessageHandlerFactory", program, StringComparison.Ordinal);
        Assert.Contains("NetworkBoundServerDbContextFactory", program, StringComparison.Ordinal);
        Assert.Contains("HumanOidcNetworkSecurity.Configure", program, StringComparison.Ordinal);
        Assert.Contains("InboundAgentTlsSecurity.Apply", program, StringComparison.Ordinal);
        Assert.Contains("AgentCertificateAuthenticationHandler", program, StringComparison.Ordinal);
        Assert.DoesNotContain(".AddCertificate(", program, StringComparison.Ordinal);
        Assert.DoesNotContain("X509RevocationMode.Online", program, StringComparison.Ordinal);
        Assert.DoesNotContain("UseNpgsql(", program, StringComparison.Ordinal);
        Assert.DoesNotContain("new HttpClientHandler", program, StringComparison.Ordinal);
    }

    /// <summary>Confirms PostgreSQL and legacy connectors receive approved literals instead of resolving configured names.</summary>
    [Fact]
    public void ProviderAndLegacyConnectorsUseOnlyPolicyApprovedAddresses()
    {
        string readiness = Read(
            "src",
            "DBNotifier.Providers.PostgreSql",
            "PostgreSqlReadinessExecutor.cs");
        string authenticated = Read(
            "src",
            "DBNotifier.Providers.PostgreSql",
            "NpgsqlAuthenticatedExecutor.cs");
        string legacy = Read("src", "modules", "DBNotifier", "DBNotifier.psm1");

        Assert.Contains("PostgreSqlNetworkEgress", readiness, StringComparison.Ordinal);
        Assert.Contains(".ResolveAsync(networkAuthorizer", readiness, StringComparison.Ordinal);
        Assert.Contains("ConnectAsync(network.ApprovedAddress", readiness, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectAsync(endpoint.Host", readiness, StringComparison.Ordinal);
        Assert.Contains("PostgreSqlNetworkEgress", authenticated, StringComparison.Ordinal);
        Assert.Contains(".ResolveAsync(networkAuthorizer", authenticated, StringComparison.Ordinal);
        Assert.Contains("ConfigureSslOptions(", authenticated, StringComparison.Ordinal);
        Assert.Contains("network.OriginalHost,", authenticated, StringComparison.Ordinal);
        Assert.Contains("serverChainPolicyFactory));", authenticated, StringComparison.Ordinal);
        Assert.Contains("Resolve-AuthorisedNetworkDestination", legacy, StringComparison.Ordinal);
        Assert.Contains("-Address $destination.Address", legacy, StringComparison.Ordinal);
        Assert.Contains("$destination.Address,", legacy, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginConnect($HostName", legacy, StringComparison.Ordinal);
        Assert.DoesNotContain("$Instance.HostName, \"-p\"", legacy, StringComparison.Ordinal);
    }

    /// <summary>Reads one repository file relative to the solution root.</summary>
    /// <param name="path">Ordered path segments beneath the repository root.</param>
    /// <returns>The complete source text.</returns>
    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([RepositoryRoot(), .. path]));

    /// <summary>Finds the repository root without depending on the test runner's working directory.</summary>
    /// <returns>The directory that contains the solution and permanent instructions.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the repository root cannot be located.</exception>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               (!File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")) ||
                !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ??
            throw new DirectoryNotFoundException("The DB-Notifier repository root could not be resolved.");
    }
}
