using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBNotifier.ConfigMigrator;

public enum MigrationItemKind
{
    Migrated,
    Defaulted,
    Rejected,
    ManualAction,
}

public sealed record MigrationReportItem(MigrationItemKind Kind, string Code, string Field);

public sealed record LegacyConfigurationMigrationRequest(
    string SourcePath,
    string TargetPath,
    bool ApplyChanges);

public sealed record LegacyConfigurationMigrationReport(
    string Status,
    bool Applied,
    string SourceSha256,
    string? TargetSha256,
    string? SourceBackupPath,
    string? PreviousTargetBackupPath,
    string? ManifestPath,
    IReadOnlyList<MigrationReportItem> Items);

public sealed record ImportedMonitoringPolicy(
    int IntervalSeconds,
    int TimeoutSeconds,
    int RetryCount,
    int RetryDelayMilliseconds,
    string PgIsReadyPath,
    string TcpFallbackEvidence);

public sealed record ImportedNotificationPolicy(bool Enabled);

public sealed record ImportedAdministrativeControl(
    bool Requested,
    string State,
    string? ServiceName);

public sealed record ImportedLegacyInstance(
    string Identity,
    string ProviderType,
    string DisplayName,
    string Host,
    int Port,
    string? PostgreSqlExecutablePath,
    bool Enabled,
    bool NotificationsEnabled,
    ImportedAdministrativeControl AdministrativeControl);

public sealed record LegacyConfigurationTarget(
    int SchemaVersion,
    string Product,
    string SourceKind,
    string SourceSha256,
    ImportedMonitoringPolicy MonitoringPolicy,
    ImportedNotificationPolicy Notifications,
    IReadOnlyList<ImportedLegacyInstance> Instances);

public sealed record LegacyConfigurationMigrationResult(
    LegacyConfigurationMigrationReport Report,
    LegacyConfigurationTarget? Target);

public sealed record LegacyConfigurationMigrationManifest(
    int SchemaVersion,
    string SourcePath,
    string TargetPath,
    string SourceSha256,
    string TargetSha256,
    string SourceBackupPath,
    string? PreviousTargetBackupPath,
    string ReportPath,
    DateTimeOffset CreatedAt);

public sealed class LegacyConfigurationMigrator(TimeProvider timeProvider)
{
    private const int MaximumSourceBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private static readonly HashSet<string> SecretFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwd", "pwd", "connectionString", "token", "secret", "privateKey", "apiKey",
    };
    private static readonly HashSet<string> RootFields = new(StringComparer.Ordinal)
    {
        "application", "logging", "notifications", "pgIsReady", "instances",
    };
    private static readonly HashSet<string> ApplicationFields = new(StringComparer.Ordinal)
    {
        "displayName", "intervalSeconds", "restartBadgeSeconds", "startMinimized", "autoDiscover", "silentMode",
    };
    private static readonly HashSet<string> LoggingFields = new(StringComparer.Ordinal) { "logPath", "level" };
    private static readonly HashSet<string> NotificationFields = new(StringComparer.Ordinal)
    {
        "enabled", "suppressStartupBalloon", "defaultBalloonTimeoutMs",
    };
    private static readonly HashSet<string> PgIsReadyFields = new(StringComparer.Ordinal)
    {
        "path", "timeoutSeconds", "retryCount", "retryDelayMs", "extraArguments",
    };
    private static readonly HashSet<string> InstanceFields = new(StringComparer.Ordinal)
    {
        "name", "serviceName", "hostName", "port", "postgresExe", "enabled", "notificationsEnabled", "restartAllowed",
    };

    public async ValueTask<LegacyConfigurationMigrationResult> MigrateAsync(
        LegacyConfigurationMigrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        string sourcePath = ValidateAbsolutePath(request.SourcePath, nameof(request.SourcePath));
        string targetPath = ValidateAbsolutePath(request.TargetPath, nameof(request.TargetPath));
        if (PathsEqual(sourcePath, targetPath))
        {
            throw new ArgumentException("Legacy source and DB-Notifier target paths must differ.", nameof(request));
        }

        byte[] sourceBytes = await ReadOrdinaryFileAsync(sourcePath, MaximumSourceBytes, cancellationToken)
            .ConfigureAwait(false);
        string sourceHash = Hash(sourceBytes);
        List<MigrationReportItem> items = [];
        LegacyConfigurationTarget? target = Parse(sourceBytes, sourceHash, items);
        if (target is null || items.Any(item => item.Kind == MigrationItemKind.Rejected))
        {
            return new(new("Rejected", false, sourceHash, null, null, null, null, items), target);
        }

        byte[] targetBytes = JsonSerializer.SerializeToUtf8Bytes(target, SerializerOptions);
        string targetHash = Hash(targetBytes);
        if (!request.ApplyChanges)
        {
            return new(new("DryRun", false, sourceHash, targetHash, null, null, null, items), target);
        }

        string? existingHash = File.Exists(targetPath)
            ? Hash(await ReadOrdinaryFileAsync(targetPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false))
            : null;
        if (string.Equals(existingHash, targetHash, StringComparison.Ordinal))
        {
            string existingManifest = targetPath + ".migration-manifest.json";
            return new(new("AlreadyCurrent", false, sourceHash, targetHash, null, null,
                File.Exists(existingManifest) ? existingManifest : null, items), target);
        }

        string? targetDirectory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException("The target directory is unavailable.");
        }

        Directory.CreateDirectory(targetDirectory);
        EnsureNoReparseDirectories(targetDirectory);
        string stamp = timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        string sourceBackupPath = targetPath + $".source-{stamp}.bak";
        string? previousTargetBackupPath = File.Exists(targetPath) ? targetPath + $".target-{stamp}.bak" : null;
        File.Copy(sourcePath, sourceBackupPath, overwrite: false);
        if (previousTargetBackupPath is not null)
        {
            File.Copy(targetPath, previousTargetBackupPath, overwrite: false);
        }

        await WriteAtomicallyAsync(targetPath, targetBytes, cancellationToken).ConfigureAwait(false);
        string manifestPath = targetPath + ".migration-manifest.json";
        string reportPath = targetPath + ".migration-report.json";
        LegacyConfigurationMigrationManifest manifest = new(
            1, sourcePath, targetPath, sourceHash, targetHash, sourceBackupPath,
            previousTargetBackupPath, reportPath, timeProvider.GetUtcNow());
        await WriteAtomicallyAsync(
            manifestPath,
            JsonSerializer.SerializeToUtf8Bytes(manifest, SerializerOptions),
            cancellationToken).ConfigureAwait(false);
        LegacyConfigurationMigrationReport report = new(
            "Applied", true, sourceHash, targetHash, sourceBackupPath,
            previousTargetBackupPath, manifestPath, items);
        await WriteAtomicallyAsync(
            reportPath,
            JsonSerializer.SerializeToUtf8Bytes(report, SerializerOptions),
            cancellationToken).ConfigureAwait(false);
        return new(report, target);
    }

    public static async ValueTask<bool> RollbackAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        string path = ValidateAbsolutePath(manifestPath, nameof(manifestPath));
        byte[] manifestBytes = await ReadOrdinaryFileAsync(path, MaximumSourceBytes, cancellationToken)
            .ConfigureAwait(false);
        LegacyConfigurationMigrationManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<LegacyConfigurationMigrationManifest>(manifestBytes, SerializerOptions)
                ?? throw new InvalidDataException("Migration manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Migration manifest is invalid.", exception);
        }

        if (manifest.SchemaVersion != 1 || !Path.IsPathFullyQualified(manifest.SourcePath) ||
            !Path.IsPathFullyQualified(manifest.TargetPath) ||
            !PathsEqual(path, manifest.TargetPath + ".migration-manifest.json") ||
            !PathsEqual(manifest.ReportPath, manifest.TargetPath + ".migration-report.json") ||
            !manifest.SourceBackupPath.StartsWith(manifest.TargetPath + ".source-", PathComparison()) ||
            !manifest.SourceBackupPath.EndsWith(".bak", StringComparison.Ordinal) ||
            (manifest.PreviousTargetBackupPath is not null &&
                (!manifest.PreviousTargetBackupPath.StartsWith(manifest.TargetPath + ".target-", PathComparison()) ||
                 !manifest.PreviousTargetBackupPath.EndsWith(".bak", StringComparison.Ordinal))) ||
            !File.Exists(manifest.TargetPath) || !File.Exists(manifest.ReportPath) ||
            !File.Exists(manifest.SourceBackupPath))
        {
            throw new InvalidDataException("Migration manifest does not match a generated target.");
        }

        string backupSourceHash = Hash(await ReadOrdinaryFileAsync(
            manifest.SourceBackupPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false));
        if (!string.Equals(backupSourceHash, manifest.SourceSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Migration source backup does not match the manifest.");
        }

        string currentHash = Hash(await ReadOrdinaryFileAsync(
            manifest.TargetPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false));
        if (!string.Equals(currentHash, manifest.TargetSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Generated target changed after migration; rollback was refused.");
        }

        if (manifest.PreviousTargetBackupPath is null)
        {
            File.Delete(manifest.TargetPath);
        }
        else
        {
            byte[] previous = await ReadOrdinaryFileAsync(
                manifest.PreviousTargetBackupPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false);
            await WriteAtomicallyAsync(manifest.TargetPath, previous, cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(manifest.ReportPath))
        {
            File.Delete(manifest.ReportPath);
        }
        File.Delete(path);
        return true;
    }

    private static LegacyConfigurationTarget? Parse(
        byte[] sourceBytes,
        string sourceHash,
        List<MigrationReportItem> items)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(sourceBytes, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32,
            });
        }
        catch (JsonException)
        {
            items.Add(new(MigrationItemKind.Rejected, "migration.json_invalid", "$"));
            return null;
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                items.Add(new(MigrationItemKind.Rejected, "migration.root_invalid", "$"));
                return null;
            }

            FindSecretFields(root, "$", items);
            ValidateFields(root, RootFields, "$", items);
            JsonElement application = ObjectOrEmpty(root, "application", ApplicationFields, items);
            _ = ObjectOrEmpty(root, "logging", LoggingFields, items);
            JsonElement notifications = ObjectOrEmpty(root, "notifications", NotificationFields, items);
            JsonElement pgIsReady = ObjectOrEmpty(root, "pgIsReady", PgIsReadyFields, items);

            int interval = ReadBoundedInt(application, "intervalSeconds", 5, 1, 86400, "$.application", items);
            int timeout = ReadBoundedInt(pgIsReady, "timeoutSeconds", 5, 1, 300, "$.pgIsReady", items);
            int retries = ReadBoundedInt(pgIsReady, "retryCount", 1, 0, 10, "$.pgIsReady", items);
            int retryDelay = ReadBoundedInt(pgIsReady, "retryDelayMs", 500, 0, 300000, "$.pgIsReady", items);
            string pgIsReadyPath = ReadString(pgIsReady, "path", "pg_isready.exe", 1024, "$.pgIsReady", items);
            if (pgIsReadyPath.Contains('\0') ||
                (!Path.IsPathFullyQualified(pgIsReadyPath) && !string.Equals(Path.GetFileName(pgIsReadyPath), pgIsReadyPath, StringComparison.Ordinal)))
            {
                items.Add(new(MigrationItemKind.Rejected, "migration.pg_isready_path_invalid", "$.pgIsReady.path"));
            }
            if (pgIsReady.ValueKind == JsonValueKind.Object &&
                pgIsReady.TryGetProperty("extraArguments", out JsonElement extraArguments) &&
                (extraArguments.ValueKind != JsonValueKind.Array || extraArguments.GetArrayLength() > 0))
            {
                items.Add(new(MigrationItemKind.Rejected, "migration.extra_arguments_unsupported", "$.pgIsReady.extraArguments"));
            }
            bool notificationsEnabled = ReadBoolean(notifications, "enabled", true, "$.notifications", items);
            List<ImportedLegacyInstance> instances = ParseInstances(root, items);
            if (items.Any(item => item.Kind == MigrationItemKind.Rejected))
            {
                return null;
            }

            items.Add(new(MigrationItemKind.Migrated, "migration.provider_postgresql", "$.instances"));
            items.Add(new(MigrationItemKind.ManualAction, "migration.tcp_fallback_degraded", "$.monitoringPolicy"));
            return new(
                1,
                "DB-Notifier",
                "PgNotifierJsonV1",
                sourceHash,
                new(interval, timeout, retries, retryDelay, pgIsReadyPath, "Degraded"),
                new(notificationsEnabled),
                instances);
        }
    }

    private static List<ImportedLegacyInstance> ParseInstances(JsonElement root, List<MigrationReportItem> items)
    {
        if (!root.TryGetProperty("instances", out JsonElement instancesElement))
        {
            items.Add(new(MigrationItemKind.Defaulted, "migration.instances_defaulted", "$.instances"));
            return [];
        }
        if (instancesElement.ValueKind != JsonValueKind.Array || instancesElement.GetArrayLength() > 1000)
        {
            items.Add(new(MigrationItemKind.Rejected, "migration.instances_invalid", "$.instances"));
            return [];
        }

        List<ImportedLegacyInstance> result = [];
        HashSet<string> identities = new(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        foreach (JsonElement instance in instancesElement.EnumerateArray())
        {
            string path = $"$.instances[{index}]";
            index++;
            if (instance.ValueKind != JsonValueKind.Object)
            {
                items.Add(new(MigrationItemKind.Rejected, "migration.instance_invalid", path));
                continue;
            }
            ValidateFields(instance, InstanceFields, path, items);
            string serviceName = ReadString(instance, "serviceName", string.Empty, 200, path, items);
            string host = ReadString(instance, "hostName", "localhost", 253, path, items);
            int port = ReadBoundedInt(instance, "port", 5432, 1, 65535, path, items);
            string identity = string.IsNullOrWhiteSpace(serviceName) ? $"{host}:{port}" : serviceName;
            if (!IsValidHost(host) || !identities.Add(identity))
            {
                items.Add(new(MigrationItemKind.Rejected, "migration.instance_identity_invalid", path));
                continue;
            }

            string displayName = ReadString(instance, "name", identity, 200, path, items);
            string executable = ReadString(instance, "postgresExe", string.Empty, 1024, path, items);
            if (!string.IsNullOrWhiteSpace(executable) && (!Path.IsPathFullyQualified(executable) || executable.Contains('\0')))
            {
                items.Add(new(MigrationItemKind.Rejected, "migration.postgres_path_invalid", path + ".postgresExe"));
            }
            bool enabled = ReadBoolean(instance, "enabled", true, path, items);
            bool instanceNotifications = ReadBoolean(instance, "notificationsEnabled", true, path, items);
            bool restartRequested = ReadBoolean(instance, "restartAllowed", false, path, items);
            if (restartRequested)
            {
                items.Add(new(MigrationItemKind.ManualAction, "migration.admin_capability_review_required",
                    path + ".restartAllowed"));
            }

            result.Add(new(
                identity,
                "postgresql",
                displayName,
                host,
                port,
                string.IsNullOrWhiteSpace(executable) ? null : executable,
                enabled,
                instanceNotifications,
                new(restartRequested, restartRequested ? "ManualActionRequired" : "Disabled",
                    string.IsNullOrWhiteSpace(serviceName) ? null : serviceName)));
        }
        return result;
    }

    private static JsonElement ObjectOrEmpty(
        JsonElement root,
        string property,
        HashSet<string> allowedFields,
        List<MigrationReportItem> items)
    {
        if (!root.TryGetProperty(property, out JsonElement value))
        {
            items.Add(new(MigrationItemKind.Defaulted, "migration.section_defaulted", "$." + property));
            return default;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            items.Add(new(MigrationItemKind.Rejected, "migration.section_invalid", "$." + property));
            return default;
        }
        ValidateFields(value, allowedFields, "$." + property, items);
        return value;
    }

    private static void ValidateFields(
        JsonElement element,
        HashSet<string> allowedFields,
        string path,
        List<MigrationReportItem> items)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!allowedFields.Contains(property.Name))
            {
                items.Add(new(MigrationItemKind.Rejected, "migration.field_unsupported", path + "." + property.Name));
            }
        }
    }

    private static void FindSecretFields(JsonElement element, string path, List<MigrationReportItem> items)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string childPath = path + "." + property.Name;
                if (SecretFields.Contains(property.Name))
                {
                    items.Add(new(MigrationItemKind.Rejected, "migration.secret_blocked", childPath));
                    continue;
                }
                FindSecretFields(property.Value, childPath, items);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement child in element.EnumerateArray())
            {
                FindSecretFields(child, $"{path}[{index}]", items);
                index++;
            }
        }
    }

    private static int ReadBoundedInt(
        JsonElement parent,
        string property,
        int defaultValue,
        int minimum,
        int maximum,
        string path,
        List<MigrationReportItem> items)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(property, out JsonElement value))
        {
            items.Add(new(MigrationItemKind.Defaulted, "migration.value_defaulted", path + "." + property));
            return defaultValue;
        }
        if (!value.TryGetInt32(out int result) || result < minimum || result > maximum)
        {
            items.Add(new(MigrationItemKind.Rejected, "migration.number_out_of_range", path + "." + property));
            return defaultValue;
        }
        return result;
    }

    private static bool ReadBoolean(
        JsonElement parent,
        string property,
        bool defaultValue,
        string path,
        List<MigrationReportItem> items)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(property, out JsonElement value))
        {
            items.Add(new(MigrationItemKind.Defaulted, "migration.value_defaulted", path + "." + property));
            return defaultValue;
        }
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            items.Add(new(MigrationItemKind.Rejected, "migration.boolean_invalid", path + "." + property));
            return defaultValue;
        }
        return value.GetBoolean();
    }

    private static string ReadString(
        JsonElement parent,
        string property,
        string defaultValue,
        int maximumLength,
        string path,
        List<MigrationReportItem> items)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(property, out JsonElement value))
        {
            items.Add(new(MigrationItemKind.Defaulted, "migration.value_defaulted", path + "." + property));
            return defaultValue;
        }
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not string result || result.Length > maximumLength)
        {
            items.Add(new(MigrationItemKind.Rejected, "migration.string_invalid", path + "." + property));
            return defaultValue;
        }
        return result;
    }

    private static string ValidateAbsolutePath(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!Path.IsPathFullyQualified(value))
        {
            throw new ArgumentException("Migration paths must be fully qualified.", parameterName);
        }
        return Path.GetFullPath(value);
    }

    private static async ValueTask<byte[]> ReadOrdinaryFileAsync(
        string path,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        FileInfo info = new(path);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
            info.Length > maximumBytes)
        {
            throw new InvalidDataException("Migration input is unavailable or violates file policy.");
        }
        EnsureNoReparseDirectories(info.DirectoryName!);
        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask WriteAtomicallyAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken)
    {
        string temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (FileStream stream = new(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string Hash(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static bool IsValidHost(string host) =>
        !string.IsNullOrWhiteSpace(host) && host.Length <= 253 &&
        Uri.CheckHostName(host) is not UriHostNameType.Unknown;

    private static void EnsureNoReparseDirectories(string directoryPath)
    {
        DirectoryInfo? directory = new(directoryPath);
        while (directory is not null && directory.Exists)
        {
            if (directory.LinkTarget is not null || directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidDataException("Migration paths cannot traverse links or reparse points.");
            }
            directory = directory.Parent;
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static bool PathsEqual(string left, string right) => string.Equals(
        Path.GetFullPath(left),
        Path.GetFullPath(right),
        PathComparison());

    private static StringComparison PathComparison() =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
