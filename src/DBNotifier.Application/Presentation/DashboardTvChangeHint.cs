// Module purpose: Defines the minimal versioned change-hint contract that can trigger, but never replace, an authoritative Dashboard TV snapshot read.
namespace DBNotifier.Application.Presentation;

/// <summary>Defines the only accepted change-hint schema and its bounded opaque revision format.</summary>
public static class DashboardTvChangeHintContract
{
    /// <summary>Gets the exact schema version supported by the restricted SignalR sandbox.</summary>
    public const string CurrentSchemaVersion = "dashboard-tv-change-hint.v1";

    /// <summary>Gets the exact prefix used by a non-secret SHA-256 projection revision.</summary>
    public const string RevisionPrefix = "sha256-";

    /// <summary>Gets the exact length of the bounded opaque projection revision.</summary>
    public const int RevisionLength = 71;

    /// <summary>Validates exact schema and lower-case SHA-256 revision syntax without treating the hint as snapshot evidence.</summary>
    /// <param name="hint">Untrusted hint received at a transport boundary.</param>
    /// <returns><see langword="true"/> only for the exact current two-field contract.</returns>
    public static bool IsValid(DashboardTvChangeHint? hint)
    {
        if (hint is null ||
            !string.Equals(hint.SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal) ||
            hint.ProjectionRevision is null ||
            hint.ProjectionRevision.Length != RevisionLength ||
            !hint.ProjectionRevision.StartsWith(RevisionPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return IsLowerHex(hint.ProjectionRevision.AsSpan(RevisionPrefix.Length));
    }

    /// <summary>Checks the canonical lower-case hexadecimal representation without accepting culture-sensitive alternatives.</summary>
    /// <param name="value">Revision suffix after the fixed algorithm prefix.</param>
    /// <returns><see langword="true"/> only when every character is a lower-case hexadecimal digit.</returns>
    private static bool IsLowerHex(ReadOnlySpan<char> value)
    {
        foreach (char character in value)
        {
            if (!char.IsAsciiHexDigitLower(character))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Contains a minimal best-effort notification that can only request a fresh API read.</summary>
/// <param name="SchemaVersion">Exact change-hint protocol version.</param>
/// <param name="ProjectionRevision">Opaque non-secret revision used only for validation and bounded correlation.</param>
public sealed record DashboardTvChangeHint(string SchemaVersion, string ProjectionRevision);
