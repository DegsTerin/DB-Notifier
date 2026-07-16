// Module purpose: Discovers approved-path readiness utilities inside the isolated PostgreSQL provider; the core remains engine-neutral.
namespace DBNotifier.Providers.PostgreSql;

/// <summary>Describes whether the approved PostgreSQL readiness executable was found safely.</summary>
public enum PostgreSqlExecutableDiscoveryState
{
    /// <summary>An ordinary file with the exact executable name exists below an approved installation root.</summary>
    Found,

    /// <summary>No approved executable was found, so the caller may use transport-only evidence.</summary>
    NotFound,

    /// <summary>The request, path or filesystem evidence violated the discovery boundary.</summary>
    Invalid,
}

/// <summary>Supplies bounded, non-secret executable discovery inputs.</summary>
/// <param name="ConfiguredPgIsReadyPath">Configured exact filename or fully qualified candidate path.</param>
/// <param name="PostgreSqlExecutablePath">Optional fully qualified PostgreSQL server executable used only to derive a sibling candidate.</param>
/// <param name="SearchPathDirectories">Parsed PATH directories considered only when they also fall below an approved root.</param>
/// <param name="InstallationRoots">Operating-system installation roots authorised for provider utility execution.</param>
public sealed record PostgreSqlExecutableDiscoveryRequest(
    string ConfiguredPgIsReadyPath,
    string? PostgreSqlExecutablePath,
    IReadOnlyList<string> SearchPathDirectories,
    IReadOnlyList<string> InstallationRoots);

/// <summary>Returns a stable discovery state, optional canonical path and sanitised reason code.</summary>
/// <param name="State">Safe discovery outcome.</param>
/// <param name="ExecutablePath">Canonical executable path only when <paramref name="State"/> is Found.</param>
/// <param name="ReasonCode">Stable non-secret diagnostic code.</param>
public sealed record PostgreSqlExecutableDiscoveryResult(
    PostgreSqlExecutableDiscoveryState State,
    string? ExecutablePath,
    string ReasonCode);

/// <summary>Resolves the PostgreSQL readiness utility without trusting arbitrary configuration or PATH roots.</summary>
public interface IPostgreSqlExecutableDiscovery
{
    /// <summary>Resolves one bounded request to a safe discovery result.</summary>
    /// <param name="request">Candidate names and approved installation roots.</param>
    /// <returns>A found, not-found or invalid result without throwing ordinary filesystem failures.</returns>
    PostgreSqlExecutableDiscoveryResult Resolve(PostgreSqlExecutableDiscoveryRequest request);
}

/// <summary>Abstracts the narrow filesystem operations needed for deterministic, link-aware discovery tests.</summary>
public interface IPostgreSqlDiscoveryFileSystem
{
    /// <summary>Checks that a candidate and its directory ancestry are ordinary non-link filesystem objects.</summary>
    /// <param name="path">Fully qualified candidate file path.</param>
    /// <returns>True only for an existing ordinary file below non-reparse directories.</returns>
    bool IsOrdinaryFile(string path);

    /// <summary>Enumerates a bounded set of immediate, ordinary child directories.</summary>
    /// <param name="path">Approved installation root.</param>
    /// <returns>At most 64 non-link immediate child paths.</returns>
    IReadOnlyList<string> GetDirectories(string path);
}

/// <summary>Implements the link-aware discovery filesystem against the local operating system.</summary>
public sealed class PostgreSqlDiscoveryFileSystem : IPostgreSqlDiscoveryFileSystem
{
    /// <inheritdoc />
    public bool IsOrdinaryFile(string path)
    {
        FileInfo info = new(path);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return false;
        }
        DirectoryInfo? directory = info.Directory;
        while (directory is not null && directory.Exists)
        {
            if (directory.LinkTarget is not null || directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return false;
            }
            directory = directory.Parent;
        }
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetDirectories(string path)
    {
        DirectoryInfo root = new(path);
        if (!root.Exists || root.LinkTarget is not null || root.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return [];
        }

        return [.. root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
            .Where(directory => directory.LinkTarget is null && !directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            .Select(directory => directory.FullName)
            .Take(64)];
    }
}

/// <summary>Resolves only the exact pg_isready utility beneath explicitly approved installation roots.</summary>
/// <param name="fileSystem">Link-aware filesystem boundary used to inspect candidates.</param>
public sealed class PostgreSqlExecutableDiscovery(IPostgreSqlDiscoveryFileSystem fileSystem)
    : IPostgreSqlExecutableDiscovery
{
    /// <inheritdoc />
    public PostgreSqlExecutableDiscoveryResult Resolve(PostgreSqlExecutableDiscoveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ConfiguredPgIsReadyPath) ||
            request.ConfiguredPgIsReadyPath.Length > 1024 || request.ConfiguredPgIsReadyPath.Contains('\0') ||
            (!Path.IsPathFullyQualified(request.ConfiguredPgIsReadyPath) &&
                !string.Equals(Path.GetFileName(request.ConfiguredPgIsReadyPath),
                    request.ConfiguredPgIsReadyPath, StringComparison.Ordinal)) ||
            request.SearchPathDirectories is null || request.InstallationRoots is null ||
            request.SearchPathDirectories.Count > 256 || request.InstallationRoots.Count > 32)
        {
            return new(PostgreSqlExecutableDiscoveryState.Invalid, null, "postgresql.discovery_request_invalid");
        }

        try
        {
            foreach (string candidate in Candidates(request).Take(256))
            {
                if (fileSystem.IsOrdinaryFile(candidate))
                {
                    return new(PostgreSqlExecutableDiscoveryState.Found, Path.GetFullPath(candidate),
                        "postgresql.pg_isready_discovered");
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new(PostgreSqlExecutableDiscoveryState.Invalid, null, "postgresql.discovery_failed_safely");
        }

        return new(PostgreSqlExecutableDiscoveryState.NotFound, null, "postgresql.pg_isready_not_found");
    }

    /// <summary>Builds a bounded runtime request from a validated endpoint and operating-system installation roots.</summary>
    /// <param name="endpoint">Validated PostgreSQL endpoint containing only non-secret executable hints.</param>
    /// <returns>A request whose PATH entries remain candidates rather than trust roots.</returns>
    public static PostgreSqlExecutableDiscoveryRequest CreateRuntimeRequest(PostgreSqlEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        string[] pathDirectories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<string> roots = [];
        AddProgramFilesRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddProgramFilesRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        AddUnixRoots(roots);
        return new(endpoint.PgIsReadyPath, endpoint.PostgreSqlExecutablePath, pathDirectories, roots);
    }

    private IEnumerable<string> Candidates(PostgreSqlExecutableDiscoveryRequest request)
    {
        HashSet<string> returned = new(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        string configured = NormaliseExecutableName(request.ConfiguredPgIsReadyPath);
        if (Path.IsPathFullyQualified(configured))
        {
            if (!IsExpectedExecutableName(configured) || !IsUnderApprovedRoot(configured, request.InstallationRoots))
            {
                yield break;
            }

            if (returned.Add(configured))
            {
                yield return configured;
            }
        }
        else if (string.Equals(Path.GetFileName(configured), configured, StringComparison.Ordinal) &&
            IsExpectedExecutableName(configured))
        {
            foreach (string directory in request.SearchPathDirectories.Where(Path.IsPathFullyQualified))
            {
                string candidate = Path.Combine(directory, configured);
                if (IsUnderApprovedRoot(candidate, request.InstallationRoots) && returned.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
        else
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(request.PostgreSqlExecutablePath) &&
            Path.IsPathFullyQualified(request.PostgreSqlExecutablePath) &&
            IsUnderApprovedRoot(request.PostgreSqlExecutablePath, request.InstallationRoots))
        {
            string? directory = Path.GetDirectoryName(request.PostgreSqlExecutablePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                string sibling = Path.Combine(directory, ExecutableName());
                if (returned.Add(sibling))
                {
                    yield return sibling;
                }
            }
        }

        foreach (string root in request.InstallationRoots.Where(Path.IsPathFullyQualified))
        {
            foreach (string versionDirectory in fileSystem.GetDirectories(root)
                .OrderByDescending(VersionKey)
                .ThenByDescending(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            {
                string candidate = Path.Combine(versionDirectory, "bin", ExecutableName());
                if (returned.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }
    }

    private static void AddProgramFilesRoot(List<string> roots, string? programFiles)
    {
        if (!string.IsNullOrWhiteSpace(programFiles) && Path.IsPathFullyQualified(programFiles))
        {
            string canonicalRoot = Path.GetFullPath(programFiles)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            roots.Add(Path.Combine(canonicalRoot, "PostgreSQL"));
        }
    }

    private static void AddUnixRoots(List<string> roots)
    {
        if (!OperatingSystem.IsWindows())
        {
            roots.Add("/usr");
            roots.Add("/usr/local");
            roots.Add("/opt/postgresql");
        }
    }

    private static string ExecutableName() => OperatingSystem.IsWindows() ? "pg_isready.exe" : "pg_isready";

    private static string NormaliseExecutableName(string configured) =>
        OperatingSystem.IsWindows() && string.Equals(configured, "pg_isready", StringComparison.OrdinalIgnoreCase)
            ? ExecutableName()
            : configured;

    private static bool IsExpectedExecutableName(string path) =>
        string.Equals(
            Path.GetFileName(path),
            ExecutableName(),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static bool IsUnderApprovedRoot(
        string path,
        IReadOnlyList<string> approvedRoots)
    {
        string candidate = Path.GetFullPath(path);
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        foreach (string root in approvedRoots.Where(Path.IsPathFullyQualified))
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string prefix = fullRoot + Path.DirectorySeparatorChar;
            if (candidate.StartsWith(prefix, comparison))
            {
                return true;
            }
        }

        return false;
    }

    private static Version VersionKey(string path) =>
        Version.TryParse(Path.GetFileName(path), out Version? version) ? version : new Version(0, 0);
}
