// Module purpose: Defines the client-verifiable assignment snapshot digest and validation boundary shared by Server and Agent.
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DBNotifier.Application.AgentFleet;

/// <summary>
/// Computes and validates the content digest of the complete read-only assignment projection. The digest contains
/// only fields transmitted to the Agent so the Agent can independently reject a body/ETag mismatch.
/// </summary>
public static class AgentAssignmentVersion
{
    private const int MaximumDisplayNameLength = 200;
    private const int MaximumProviderTypeLength = 64;
    private const int MaximumEnvironmentLength = 100;
    private const int MaximumCredentialReferenceLength = 500;

    /// <summary>Computes the uppercase SHA-256 digest of one complete assignment snapshot.</summary>
    /// <param name="agentId">Agent that owns the snapshot.</param>
    /// <param name="assignments">Complete assignments in their transmitted deterministic order.</param>
    /// <returns>An uppercase 64-character SHA-256 hexadecimal digest.</returns>
    public static string Compute(Guid agentId, IReadOnlyList<AgentReadOnlyAssignment> assignments)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(assignments);

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, AgentFleetProtocol.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture));
        Append(hash, agentId.ToString("D"));
        Append(hash, assignments.Count.ToString(CultureInfo.InvariantCulture));
        foreach (AgentReadOnlyAssignment assignment in assignments)
        {
            Append(hash, assignment.InstanceId.ToString("D"));
            Append(hash, assignment.DisplayName);
            Append(hash, assignment.ProviderType);
            Append(hash, assignment.Environment);
            Append(hash, CanonicaliseJson(assignment.Endpoint));
            Append(hash, assignment.MonitoringCredentialReference ?? string.Empty);
            Append(hash, CanonicaliseJson(assignment.Tags));
            Append(hash, assignment.IntervalSeconds.ToString(CultureInfo.InvariantCulture));
            Append(hash, assignment.TimeoutSeconds.ToString(CultureInfo.InvariantCulture));
            Append(hash, assignment.RetryCount.ToString(CultureInfo.InvariantCulture));
            Append(hash, assignment.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    /// <summary>Validates a complete snapshot before it can replace the Agent's last-known-valid configuration.</summary>
    /// <param name="snapshot">Deserialised snapshot returned by the authenticated API.</param>
    /// <param name="expectedAgentId">Exact locally enrolled Agent identifier.</param>
    /// <param name="expectedEnvironment">Exact locally enrolled environment.</param>
    /// <param name="entityTag">Strong HTTP ETag returned with the body.</param>
    /// <param name="errorCode">Stable sanitised validation failure, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> only when the complete projection and digest are valid.</returns>
    public static bool TryValidate(
        AgentAssignmentSnapshot snapshot,
        Guid expectedAgentId,
        string expectedEnvironment,
        string? entityTag,
        out string? errorCode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        errorCode = "assignments.snapshot_invalid";
        if (expectedAgentId == Guid.Empty || string.IsNullOrWhiteSpace(expectedEnvironment) ||
            snapshot.SchemaVersion != AgentFleetProtocol.CurrentSchemaVersion ||
            snapshot.AgentId != expectedAgentId || snapshot.Assignments is null ||
            snapshot.Assignments.Count > AgentFleetProtocol.MaximumAssignments ||
            !IsUpperHexDigest(snapshot.Version) || !MatchesStrongEntityTag(snapshot.Version, entityTag))
        {
            return false;
        }

        HashSet<Guid> instanceIds = [];
        long aggregateBytes = 0;
        foreach (AgentReadOnlyAssignment assignment in snapshot.Assignments)
        {
            if (assignment is null || assignment.InstanceId == Guid.Empty ||
                !instanceIds.Add(assignment.InstanceId) ||
                !IsBoundedText(assignment.DisplayName, MaximumDisplayNameLength) ||
                !IsBoundedText(assignment.ProviderType, MaximumProviderTypeLength) ||
                !string.Equals(assignment.Environment, expectedEnvironment, StringComparison.Ordinal) ||
                assignment.Environment.Length > MaximumEnvironmentLength ||
                assignment.Endpoint.ValueKind != JsonValueKind.Object ||
                assignment.Tags.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object) ||
                assignment.IntervalSeconds is < 1 or > 86_400 ||
                assignment.TimeoutSeconds is < 1 or > 300 ||
                assignment.RetryCount is < 0 or > 9 ||
                assignment.UpdatedAt == default ||
                (assignment.MonitoringCredentialReference is not null &&
                    !IsBoundedText(assignment.MonitoringCredentialReference, MaximumCredentialReferenceLength)))
            {
                return false;
            }

            try
            {
                aggregateBytes = checked(
                    aggregateBytes +
                    Encoding.UTF8.GetByteCount(assignment.Endpoint.GetRawText()) +
                    Encoding.UTF8.GetByteCount(assignment.Tags.GetRawText()));
            }
            catch (OverflowException)
            {
                errorCode = "assignments.limit_exceeded";
                return false;
            }

            if (aggregateBytes > AgentFleetProtocol.MaximumAssignmentPayloadBytes)
            {
                errorCode = "assignments.limit_exceeded";
                return false;
            }
        }

        string computed = Compute(snapshot.AgentId, snapshot.Assignments);
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(computed),
                Convert.FromHexString(snapshot.Version)))
        {
            errorCode = "assignments.digest_mismatch";
            return false;
        }

        errorCode = null;
        return true;
    }

    /// <summary>Checks the canonical uppercase representation used by assignment versions.</summary>
    /// <param name="value">Candidate digest.</param>
    /// <returns><see langword="true"/> only for 64 uppercase hexadecimal characters.</returns>
    public static bool IsUpperHexDigest(string? value) =>
        value is { Length: 64 } && value.All(character =>
            character is >= '0' and <= '9' or >= 'A' and <= 'F');

    private static bool MatchesStrongEntityTag(string version, string? entityTag) =>
        entityTag is { Length: 66 } && entityTag[0] == '"' && entityTag[^1] == '"' &&
        string.Equals(entityTag[1..^1], version, StringComparison.Ordinal);

    private static bool IsBoundedText(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;

    private static string CanonicaliseJson(JsonElement value) => JsonSerializer.Serialize(value);

    private static void Append(IncrementalHash hash, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }
}
