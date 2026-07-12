namespace DBNotifier.Providers.PostgreSql;

public enum PostgreSqlExecutableDiscoveryState
{
    Found,
    NotFound,
    Invalid,
}

public sealed record PostgreSqlExecutableDiscoveryRequest(
    string ConfiguredPgIsReadyPath,
    string? PostgreSqlExecutablePath,
    IReadOnlyList<string> SearchPathDirectories,
    IReadOnlyList<string> InstallationRoots);

public sealed record PostgreSqlExecutableDiscoveryResult(
    PostgreSqlExecutableDiscoveryState State,
    string? ExecutablePath,
    string ReasonCode);

public interface IPostgreSqlExecutableDiscovery
{
    PostgreSqlExecutableDiscoveryResult Resolve(PostgreSqlExecutableDiscoveryRequest request);
}

public interface IPostgreSqlDiscoveryFileSystem
{
    bool IsOrdinaryFile(string path);

    IReadOnlyList<string> GetDirectories(string path);
}

public sealed class PostgreSqlDiscoveryFileSystem : IPostgreSqlDiscoveryFileSystem
{
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

public sealed class PostgreSqlExecutableDiscovery(IPostgreSqlDiscoveryFileSystem fileSystem)
    : IPostgreSqlExecutableDiscovery
{
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

    public static PostgreSqlExecutableDiscoveryRequest CreateRuntimeRequest(PostgreSqlEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        string[] pathDirectories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<string> roots = [];
        AddProgramFilesRoot(roots, Environment.GetEnvironmentVariable("ProgramFiles"));
        AddProgramFilesRoot(roots, Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
        return new(endpoint.PgIsReadyPath, endpoint.PostgreSqlExecutablePath, pathDirectories, roots);
    }

    private IEnumerable<string> Candidates(PostgreSqlExecutableDiscoveryRequest request)
    {
        HashSet<string> returned = new(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        string configured = request.ConfiguredPgIsReadyPath;
        if (Path.IsPathFullyQualified(configured))
        {
            if (returned.Add(configured))
            {
                yield return configured;
            }
        }
        else if (string.Equals(Path.GetFileName(configured), configured, StringComparison.Ordinal))
        {
            foreach (string directory in request.SearchPathDirectories.Where(Path.IsPathFullyQualified))
            {
                string candidate = Path.Combine(directory, configured);
                if (returned.Add(candidate))
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
            Path.IsPathFullyQualified(request.PostgreSqlExecutablePath))
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
            roots.Add(Path.Combine(programFiles, "PostgreSQL"));
        }
    }

    private static string ExecutableName() => OperatingSystem.IsWindows() ? "pg_isready.exe" : "pg_isready";

    private static Version VersionKey(string path) =>
        Version.TryParse(Path.GetFileName(path), out Version? version) ? version : new Version(0, 0);
}
