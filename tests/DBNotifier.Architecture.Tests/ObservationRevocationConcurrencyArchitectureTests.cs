// Module purpose: Guards the shared process and PostgreSQL ordering boundaries between observation ingestion and Agent revocation.
namespace DBNotifier.Architecture.Tests;

/// <summary>Prevents either participant from bypassing the authoritative Agent identity transaction fence.</summary>
public sealed class ObservationRevocationConcurrencyArchitectureTests
{
    /// <summary>Confirms both stores share one gate and preserve the required authority-decision order.</summary>
    [Fact]
    public void IngestionAndRevocationUseTheSameAgentIdentityFenceInAuthorityOrder()
    {
        string fence = Read(
            "src",
            "DBNotifier.Persistence.Server.PostgreSql",
            "AgentIdentityTransactionFence.cs");
        string ingestion = Read(
            "src",
            "DBNotifier.Persistence.Server.PostgreSql",
            "ServerObservationIngestionStore.cs");
        string agentFleet = Read(
            "src",
            "DBNotifier.Persistence.Server.PostgreSql",
            "AgentFleetStore.cs");

        Assert.Contains("FOR NO KEY UPDATE", fence, StringComparison.Ordinal);

        string ingestEntry = ExtractBlockAfter(
            ingestion,
            "public async ValueTask<ObservationItemResult> IngestAsync(");
        AssertOrdered(
            ingestEntry,
            "AgentIdentityTransactionFence",
            ".EnterAsync(message.AgentId",
            "IngestCoreAsync(message");

        string ingestTransaction = ExtractBlockAfter(
            ingestion,
            "private async ValueTask<ObservationItemResult> IngestCoreAsync(");
        AssertOrdered(
            ingestTransaction,
            "BeginTransactionAsync(IsolationLevel.Serializable",
            ".LockIdentityAsync(context, message.AgentId",
            "if (agent is null)",
            "if (!string.Equals(agent.State, \"Active\"",
            "context.HealthSamples.Add(",
            "transaction.CommitAsync(");

        string persistenceClassification = ExtractBlockAfter(
            ingestion,
            "private async ValueTask<ObservationItemResult> ClassifyAfterPersistenceFailureAsync(");
        AssertOrdered(
            persistenceClassification,
            "BeginTransactionAsync(IsolationLevel.Serializable",
            ".LockIdentityAsync(verification, message.AgentId",
            "ClassifyExistingAsync(",
            "if (concurrentResult is not null)",
            "if (agent is not null",
            "ConsumeRejectedInTransactionAsync(",
            "verification,",
            "transaction,");
        Assert.DoesNotContain(
            "ConsumeRejectedCoreAsync(",
            persistenceClassification,
            StringComparison.Ordinal);

        string revocationFence = ExtractBlockAfter(
            agentFleet,
            "using (IDisposable identityFence = await AgentIdentityTransactionFence");
        Assert.Contains("RevokeIdentityAsync(", revocationFence, StringComparison.Ordinal);

        string revocationTransaction = ExtractBlockAfter(
            agentFleet,
            "private async ValueTask<AgentRevocationOutcome> RevokeIdentityAsync(");
        AssertOrdered(
            revocationTransaction,
            "BeginTransactionAsync(IsolationLevel.Serializable",
            "agent.State = \"Revoked\"",
            "context.SaveChangesAsync(",
            "transaction.CommitAsync(");
    }

    /// <summary>Asserts that required source tokens appear in their security-significant lexical order.</summary>
    /// <param name="source">Source block that owns the ordering contract.</param>
    /// <param name="tokens">Ordered tokens that identify the guarded operations.</param>
    private static void AssertOrdered(string source, params string[] tokens)
    {
        int previousIndex = -1;
        foreach (string token in tokens)
        {
            int currentIndex = source.IndexOf(token, previousIndex + 1, StringComparison.Ordinal);
            Assert.True(
                currentIndex > previousIndex,
                $"Expected '{token}' after source offset {previousIndex}.");
            previousIndex = currentIndex;
        }
    }

    /// <summary>Extracts the balanced C# block that follows one exact source marker.</summary>
    /// <param name="source">Complete source file text.</param>
    /// <param name="marker">Exact declaration or statement immediately before the required block.</param>
    /// <returns>The block contents, including its outer braces.</returns>
    private static string ExtractBlockAfter(string source, string marker)
    {
        int markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"Expected source marker '{marker}'.");

        int blockStart = source.IndexOf('{', markerIndex + marker.Length);
        Assert.True(blockStart >= 0, $"Expected a block after source marker '{marker}'.");

        int depth = 0;
        for (int index = blockStart; index < source.Length; index++)
        {
            depth += source[index] switch
            {
                '{' => 1,
                '}' => -1,
                _ => 0,
            };
            if (depth == 0)
            {
                return source[blockStart..(index + 1)];
            }
        }

        throw new InvalidDataException($"Source block after marker '{marker}' is not balanced.");
    }

    /// <summary>Reads one repository file from the root resolved by the solution and permanent instructions.</summary>
    /// <param name="path">Path segments beneath the repository root.</param>
    /// <returns>The complete text of the requested source file.</returns>
    private static string Read(params string[] path) => File.ReadAllText(
        Path.Combine([RepositoryRoot(), .. path]));

    /// <summary>Finds the repository root without depending on the current test working directory.</summary>
    /// <returns>The directory containing the solution and permanent instructions.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the repository root cannot be resolved.</exception>
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
