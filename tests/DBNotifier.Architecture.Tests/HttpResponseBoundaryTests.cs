// Module purpose: Verifies every product HTTP JSON response reader retains the shared bounded materialisation boundary.
namespace DBNotifier.Architecture.Tests;

/// <summary>Protects the repository-wide inventory of HTTP JSON consumers from unbounded convenience APIs.</summary>
public sealed class HttpResponseBoundaryTests
{
    /// <summary>Proves every current C# response consumer delegates byte and content-type admission to one shared reader.</summary>
    [Fact]
    public void CSharpHttpJsonReadersUseTheBoundedSharedBoundary()
    {
        string root = RepositoryRoot();
        string sourceRoot = Path.Combine(root, "src");
        string[] readers =
        [
            "src/DBNotifier.Infrastructure/AgentFleet/HttpAgentFleetClientTransport.cs",
            "src/DBNotifier.Infrastructure/Presentation/ReconciledLocalNotificationAdapters.cs",
            "src/DBNotifier.Infrastructure/Synchronization/HttpCommandDeliveryTransport.cs",
            "src/DBNotifier.Infrastructure/Synchronization/HttpCommandTransportSandboxClient.cs",
            "src/DBNotifier.Infrastructure/Synchronization/HttpObservationBatchTransport.cs",
        ];

        foreach (string relativePath in readers)
        {
            string source = File.ReadAllText(Path.Combine([root, .. relativePath.Split('/')]));
            Assert.Contains("BoundedHttpJsonReader", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadFromJsonAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadAsStringAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadAsByteArrayAsync", source, StringComparison.Ordinal);
        }

        string boundedReaderPath = Path.GetFullPath(Path.Combine(
            sourceRoot,
            "DBNotifier.Infrastructure",
            "Http",
            "BoundedHttpJsonReader.cs"));
        foreach (string sourcePath in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(sourcePath);
            Assert.DoesNotContain("ReadFromJsonAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadAsStringAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadAsByteArrayAsync", source, StringComparison.Ordinal);
            if (!string.Equals(Path.GetFullPath(sourcePath), boundedReaderPath, StringComparison.OrdinalIgnoreCase))
            {
                Assert.DoesNotContain("ReadAsStreamAsync", source, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>Proves the browser adapter streams and counts bytes instead of calling the unbounded text helper.</summary>
    [Fact]
    public void DashboardTvHttpReaderStreamsUnderItsByteCeiling()
    {
        string source = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "DBNotifier.Dashboard.Web",
            "src",
            "dashboardTvReconciliation.ts"));

        Assert.Contains("readBoundedUtf8Body", source, StringComparison.Ordinal);
        Assert.Contains("response.body.getReader()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("response.text()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("response.json()", source, StringComparison.Ordinal);
    }

    /// <summary>Proves R3 validation did not make the normal issuer, provider distribution or Agent client activatable.</summary>
    [Fact]
    public void NormalAgentFleetCompositionRemainsUnavailable()
    {
        string root = RepositoryRoot();
        string serverProgram = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Server.Api", "Program.cs"));
        string agentProgram = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Agent.Worker", "Program.cs"));
        string clientOptions = File.ReadAllText(Path.Combine(
            root,
            "src",
            "DBNotifier.Agent.Worker",
            "AgentFleetClientOptions.cs"));

        Assert.Contains("new ProviderRegistry([])", serverProgram, StringComparison.Ordinal);
        Assert.Contains("UnavailableAgentCertificateIssuer", serverProgram, StringComparison.Ordinal);
        Assert.Contains("agentFleetClientOptions.ValidateForStartup()", agentProgram, StringComparison.Ordinal);
        Assert.Contains("if (Enabled)", clientOptions, StringComparison.Ordinal);
        Assert.Contains("agent_fleet.sandbox_only", clientOptions, StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
