// Module purpose: Guards disabled-default composition and the execution-ineligible boundary of the command-transport safety sandbox.
namespace DBNotifier.Architecture.Tests;

/// <summary>Protects the isolated v2 transport from normal API or Agent Worker activation.</summary>
public sealed class CommandTransportSafetyIsolationTests
{
    /// <summary>Confirms the normal Server API never registers or maps the sandbox transport.</summary>
    [Fact]
    public void NormalApiDoesNotComposeCommandTransportSafetySandbox()
    {
        string program = Read("src", "DBNotifier.Server.Api", "Program.cs");
        string containment = Read("src", "DBNotifier.Server.Api", "CommandSurfaceContainmentEndpoints.cs");

        Assert.DoesNotContain("ICommandTransportServerStore", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapCommandTransportSafetySandboxEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/v2/sandbox/", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IServerCommandDeliveryStore", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ServerCommandDeliveryStore", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCommandAsync", program, StringComparison.Ordinal);
        Assert.Contains("MapContainedCommandSurface", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IServerCommandDeliveryStore", containment, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuthorizedOperationsStore", containment, StringComparison.Ordinal);
        Assert.DoesNotContain("AuthorizedOperationsService", containment, StringComparison.Ordinal);
    }

    /// <summary>Confirms ordinary command polling remains explicitly unavailable at Agent Worker startup.</summary>
    [Fact]
    public void NormalAgentWorkerKeepsCommandPollingFailClosed()
    {
        string options = Read("src", "DBNotifier.Agent.Worker", "AgentSynchronizationOptions.cs");
        string program = Read("src", "DBNotifier.Agent.Worker", "Program.cs");

        Assert.Contains("command.polling_durable_protocol_unavailable", options, StringComparison.Ordinal);
        Assert.Contains("ValidateCommandPollingForStartup", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AgentCommandPollingWorker", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IAgentCommandInboxStore", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ICommandDeliveryTransport", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpCommandDeliveryTransport", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CommandTransportSandboxCoordinator", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpCommandTransportSandboxClient", program, StringComparison.Ordinal);
    }

    /// <summary>Confirms only the explicit test harness switch can enter the temporary child-process path.</summary>
    [Fact]
    public void ChildHarnessRequiresExactSandboxSwitch()
    {
        string program = Read("tests", "DBNotifier.AgentFleet.SandboxHost", "Program.cs");
        string commandHost = Read(
            "tests",
            "DBNotifier.AgentFleet.SandboxHost",
            "CommandTransportSandboxHost.cs");
        string agentStore = Read(
            "src",
            "DBNotifier.Persistence.Agent.Sqlite",
            "AgentCommandTransportSandboxStore.cs");
        string sandboxContext = Read(
            "src",
            "DBNotifier.Persistence.Agent.Sqlite",
            "AgentCommandTransportSandboxDbContext.cs");

        Assert.Contains("--sandbox-command-transport", program, StringComparison.Ordinal);
        Assert.Contains("--sandbox-command-transport", commandHost, StringComparison.Ordinal);
        Assert.Contains("CommandExecutionPolicy.Never", agentStore, StringComparison.Ordinal);
        Assert.Contains("AgentCommandTransportSandboxDbContext", commandHost, StringComparison.Ordinal);
        Assert.Contains("execution_policy = 'Never'", sandboxContext, StringComparison.Ordinal);
        Assert.Contains("sandbox_command_receipts", sandboxContext, StringComparison.Ordinal);
        Assert.DoesNotContain("AgentDbContext context", agentStore, StringComparison.Ordinal);
        Assert.DoesNotContain("InboxCommands", agentStore, StringComparison.Ordinal);
        Assert.DoesNotContain("CommandAttempt", commandHost, StringComparison.Ordinal);
    }

    private static string Read(params string[] path) => File.ReadAllText(
        Path.Combine([RepositoryRoot(), .. path]));

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
