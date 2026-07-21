// Module purpose: Implements Legacy Configuration Migration for isolated, fail-closed legacy configuration migration.
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32.SafeHandles;

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
    string? PreviousTargetSha256,
    string SourceBackupPath,
    string SourceBackupSha256,
    string? PreviousTargetBackupPath,
    string? PreviousTargetBackupSha256,
    string ReportPath,
    string ReportSha256,
    string JournalPath,
    DateTimeOffset CreatedAt);

internal enum MigrationFaultPoint
{
    AfterJournalCreated,
    AfterTargetCommitted,
    AfterReportCommitted,
    AfterManifestCommitted,
    AfterRollbackJournalCreated,
    AfterRollbackTargetCommitted,
    AfterRollbackReportRemoved,
    AfterRollbackManifestRemoved,
}

internal sealed record LegacyConfigurationMigrationJournal(
    int SchemaVersion,
    string State,
    string SourcePath,
    string TargetPath,
    string SourceSha256,
    string TargetSha256,
    string? PreviousTargetSha256,
    string SourceBackupPath,
    string SourceBackupSha256,
    string? PreviousTargetBackupPath,
    string? PreviousTargetBackupSha256,
    string ManifestPath,
    string ManifestSha256,
    string ReportPath,
    string ReportSha256,
    string PendingTargetPath,
    string PendingManifestPath,
    string PendingReportPath,
    DateTimeOffset CreatedAt);

public sealed class LegacyConfigurationMigrator
{
    private const int MaximumSourceBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly TimeProvider timeProvider;
    private readonly Action<MigrationFaultPoint>? faultInjector;
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

    public LegacyConfigurationMigrator(TimeProvider timeProvider)
        : this(timeProvider, null)
    {
    }

    internal LegacyConfigurationMigrator(
        TimeProvider timeProvider,
        Action<MigrationFaultPoint>? faultInjector)
    {
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.faultInjector = faultInjector;
    }

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

        EnsureDistinctPathAliases(sourcePath, targetPath);
        await RecoverIfRequiredAsync(targetPath, cancellationToken).ConfigureAwait(false);

        SecureFileSnapshot sourceSnapshot = await ReadOrdinaryFileAsync(sourcePath, MaximumSourceBytes, cancellationToken)
            .ConfigureAwait(false);
        byte[] sourceBytes = sourceSnapshot.Content;
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

        SecureFileSnapshot? existingTarget = File.Exists(targetPath)
            ? await ReadOrdinaryFileAsync(targetPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false)
            : null;
        string? existingHash = existingTarget?.Sha256;
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
        string manifestPath = targetPath + ".migration-manifest.json";
        string reportPath = targetPath + ".migration-report.json";
        string journalPath = targetPath + ".migration-journal.json";
        string pendingTargetPath = targetPath + $".pending-{stamp}";
        string pendingManifestPath = manifestPath + $".pending-{stamp}";
        string pendingReportPath = reportPath + $".pending-{stamp}";
        ValidateDerivedPaths(targetPath, sourceBackupPath, previousTargetBackupPath, manifestPath, reportPath,
            journalPath, pendingTargetPath, pendingManifestPath, pendingReportPath);
        if (File.Exists(manifestPath) || File.Exists(reportPath) || File.Exists(journalPath))
        {
            throw new InvalidDataException("Existing migration control files require rollback before another migration.");
        }

        string? previousHash = existingTarget?.Sha256;
        await WriteNewDurablyAsync(sourceBackupPath, sourceBytes, cancellationToken).ConfigureAwait(false);
        if (previousTargetBackupPath is not null && existingTarget is not null)
        {
            await WriteNewDurablyAsync(previousTargetBackupPath, existingTarget.Content, cancellationToken)
                .ConfigureAwait(false);
        }

        LegacyConfigurationMigrationReport report = new(
            "Applied", true, sourceHash, targetHash, sourceBackupPath,
            previousTargetBackupPath, manifestPath, items);
        byte[] reportBytes = JsonSerializer.SerializeToUtf8Bytes(report, SerializerOptions);
        string reportHash = Hash(reportBytes);
        LegacyConfigurationMigrationManifest manifest = new(
            2, sourcePath, targetPath, sourceHash, targetHash, previousHash, sourceBackupPath,
            sourceHash, previousTargetBackupPath, previousHash, reportPath, reportHash, journalPath,
            timeProvider.GetUtcNow());
        byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, SerializerOptions);
        string manifestHash = Hash(manifestBytes);
        await WriteNewDurablyAsync(pendingTargetPath, targetBytes, cancellationToken).ConfigureAwait(false);
        await WriteNewDurablyAsync(pendingManifestPath, manifestBytes, cancellationToken).ConfigureAwait(false);
        await WriteNewDurablyAsync(pendingReportPath, reportBytes, cancellationToken).ConfigureAwait(false);
        LegacyConfigurationMigrationJournal journal = new(
            1, "Prepared", sourcePath, targetPath, sourceHash, targetHash, previousHash,
            sourceBackupPath, sourceHash, previousTargetBackupPath, previousHash,
            manifestPath, manifestHash, reportPath, reportHash,
            pendingTargetPath, pendingManifestPath, pendingReportPath, timeProvider.GetUtcNow());
        await WriteNewDurablyAsync(
            journalPath,
            JsonSerializer.SerializeToUtf8Bytes(journal, SerializerOptions),
            cancellationToken).ConfigureAwait(false);
        faultInjector?.Invoke(MigrationFaultPoint.AfterJournalCreated);

        await RevalidateSnapshotAsync(sourceSnapshot, cancellationToken).ConfigureAwait(false);
        await RevalidateOptionalSnapshotAsync(targetPath, existingTarget, cancellationToken).ConfigureAwait(false);
        CommitStagedFile(pendingTargetPath, targetPath);
        faultInjector?.Invoke(MigrationFaultPoint.AfterTargetCommitted);
        CommitStagedFile(pendingReportPath, reportPath);
        faultInjector?.Invoke(MigrationFaultPoint.AfterReportCommitted);
        CommitStagedFile(pendingManifestPath, manifestPath);
        faultInjector?.Invoke(MigrationFaultPoint.AfterManifestCommitted);
        await WriteAtomicallyAsync(
            journalPath,
            JsonSerializer.SerializeToUtf8Bytes(journal with { State = "Completed" }, SerializerOptions),
            cancellationToken).ConfigureAwait(false);
        return new(report, target);
    }

    public static ValueTask<bool> RollbackAsync(
        string manifestPath,
        CancellationToken cancellationToken = default) =>
        RollbackAsync(manifestPath, null, cancellationToken);

    internal static async ValueTask<bool> RollbackAsync(
        string manifestPath,
        Action<MigrationFaultPoint>? faultInjector,
        CancellationToken cancellationToken = default)
    {
        string path = ValidateAbsolutePath(manifestPath, nameof(manifestPath));
        SecureFileSnapshot manifestSnapshot = await ReadOrdinaryFileAsync(path, MaximumSourceBytes, cancellationToken)
            .ConfigureAwait(false);
        LegacyConfigurationMigrationManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<LegacyConfigurationMigrationManifest>(manifestSnapshot.Content, SerializerOptions)
                ?? throw new InvalidDataException("Migration manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Migration manifest is invalid.", exception);
        }

        if (manifest.SchemaVersion != 2 || !Path.IsPathFullyQualified(manifest.SourcePath) ||
            !Path.IsPathFullyQualified(manifest.TargetPath) ||
            !PathsEqual(path, manifest.TargetPath + ".migration-manifest.json") ||
            !PathsEqual(manifest.ReportPath, manifest.TargetPath + ".migration-report.json") ||
            !PathsEqual(manifest.JournalPath, manifest.TargetPath + ".migration-journal.json") ||
            !manifest.SourceBackupPath.StartsWith(manifest.TargetPath + ".source-", PathComparison()) ||
            !manifest.SourceBackupPath.EndsWith(".bak", StringComparison.Ordinal) ||
            (manifest.PreviousTargetBackupPath is not null &&
                (!manifest.PreviousTargetBackupPath.StartsWith(manifest.TargetPath + ".target-", PathComparison()) ||
                 !manifest.PreviousTargetBackupPath.EndsWith(".bak", StringComparison.Ordinal))) ||
            !File.Exists(manifest.TargetPath) || !File.Exists(manifest.ReportPath) || !File.Exists(manifest.JournalPath) ||
            !File.Exists(manifest.SourceBackupPath))
        {
            throw new InvalidDataException("Migration manifest does not match a generated target.");
        }

        SecureFileSnapshot journalSnapshot = await ReadOrdinaryFileAsync(
            manifest.JournalPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false);
        LegacyConfigurationMigrationJournal journal = DeserializeJournal(journalSnapshot.Content);
        ValidateJournal(journal, manifest.TargetPath);
        if (journal.State != "Completed" || !string.Equals(manifestSnapshot.Sha256, journal.ManifestSha256, StringComparison.Ordinal) ||
            !string.Equals(manifest.ReportSha256, journal.ReportSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Migration control files do not authenticate one another.");
        }

        SecureFileSnapshot backupSource = await ReadOrdinaryFileAsync(
            manifest.SourceBackupPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(backupSource.Sha256, manifest.SourceBackupSha256, StringComparison.Ordinal) ||
            !string.Equals(backupSource.Sha256, manifest.SourceSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Migration source backup does not match the manifest.");
        }

        SecureFileSnapshot reportSnapshot = await ReadOrdinaryFileAsync(
            manifest.ReportPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false);
        SecureFileSnapshot currentTarget = await ReadOrdinaryFileAsync(
            manifest.TargetPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false);
        SecureFileSnapshot? previousTarget = manifest.PreviousTargetBackupPath is null
            ? null
            : await ReadOrdinaryFileAsync(manifest.PreviousTargetBackupPath, MaximumSourceBytes, cancellationToken)
                .ConfigureAwait(false);
        if (!string.Equals(reportSnapshot.Sha256, manifest.ReportSha256, StringComparison.Ordinal) ||
            !string.Equals(currentTarget.Sha256, manifest.TargetSha256, StringComparison.Ordinal) ||
            !string.Equals(previousTarget?.Sha256, manifest.PreviousTargetBackupSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Generated target changed after migration; rollback was refused.");
        }

        await RevalidateSnapshotAsync(currentTarget, cancellationToken).ConfigureAwait(false);
        if (previousTarget is not null)
        {
            await RevalidateSnapshotAsync(previousTarget, cancellationToken).ConfigureAwait(false);
        }

        LegacyConfigurationMigrationJournal rollingBack = journal with { State = "RollingBack" };
        await WriteAtomicallyAsync(
            manifest.JournalPath,
            JsonSerializer.SerializeToUtf8Bytes(rollingBack, SerializerOptions),
            cancellationToken).ConfigureAwait(false);
        faultInjector?.Invoke(MigrationFaultPoint.AfterRollbackJournalCreated);
        await CompleteRollbackAsync(rollingBack, faultInjector, cancellationToken).ConfigureAwait(false);
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
            items.Add(new(MigrationItemKind.Migrated, "migration.pg_isready_legacy_input_consumed", "$.pgIsReady"));
            items.Add(new(MigrationItemKind.ManualAction, "migration.tcp_fallback_degraded", "$.monitoringPolicy"));
            return new(
                1,
                "DB-Notifier",
                "PgNotifierJsonV1",
                sourceHash,
                new(interval, timeout, retries, retryDelay, "Degraded"),
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

    private static async ValueTask RecoverIfRequiredAsync(string targetPath, CancellationToken cancellationToken)
    {
        string journalPath = targetPath + ".migration-journal.json";
        if (!File.Exists(journalPath))
        {
            return;
        }

        SecureFileSnapshot journalSnapshot = await ReadOrdinaryFileAsync(
            journalPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false);
        LegacyConfigurationMigrationJournal journal = DeserializeJournal(journalSnapshot.Content);
        ValidateJournal(journal, targetPath);
        if (journal.State == "Completed")
        {
            await VerifyCompletedMigrationAsync(journal, cancellationToken).ConfigureAwait(false);
            await DeleteExpectedFileAsync(
                journal.PendingTargetPath, journal.TargetSha256, cancellationToken).ConfigureAwait(false);
            await DeleteExpectedFileAsync(
                journal.PendingManifestPath, journal.ManifestSha256, cancellationToken).ConfigureAwait(false);
            await DeleteExpectedFileAsync(
                journal.PendingReportPath, journal.ReportSha256, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (journal.State == "RollingBack")
        {
            await CompleteRollbackAsync(journal, null, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (journal.State != "Prepared")
        {
            throw new InvalidDataException("The migration journal state is unsupported.");
        }

        await VerifySnapshotHashAsync(journal.SourceBackupPath, journal.SourceBackupSha256, cancellationToken)
            .ConfigureAwait(false);
        if (journal.PreviousTargetBackupPath is not null && journal.PreviousTargetBackupSha256 is not null)
        {
            await VerifySnapshotHashAsync(
                journal.PreviousTargetBackupPath, journal.PreviousTargetBackupSha256, cancellationToken)
                .ConfigureAwait(false);
        }

        string? currentTargetHash = File.Exists(targetPath)
            ? (await ReadOrdinaryFileAsync(targetPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false)).Sha256
            : null;
        if (string.Equals(currentTargetHash, journal.TargetSha256, StringComparison.Ordinal))
        {
            await PromoteOrVerifyAsync(
                journal.PendingReportPath, journal.ReportPath, journal.ReportSha256, cancellationToken)
                .ConfigureAwait(false);
            await PromoteOrVerifyAsync(
                journal.PendingManifestPath, journal.ManifestPath, journal.ManifestSha256, cancellationToken)
                .ConfigureAwait(false);
            DeleteIfExists(journal.PendingTargetPath);
            await WriteAtomicallyAsync(
                journalPath,
                JsonSerializer.SerializeToUtf8Bytes(journal with { State = "Completed" }, SerializerOptions),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (string.Equals(currentTargetHash, journal.PreviousTargetSha256, StringComparison.Ordinal))
        {
            await DeleteExpectedFileAsync(
                journal.PendingTargetPath, journal.TargetSha256, cancellationToken).ConfigureAwait(false);
            await DeleteExpectedFileAsync(
                journal.PendingManifestPath, journal.ManifestSha256, cancellationToken).ConfigureAwait(false);
            await DeleteExpectedFileAsync(
                journal.PendingReportPath, journal.ReportSha256, cancellationToken).ConfigureAwait(false);
            DeleteIfExists(journal.SourceBackupPath);
            if (journal.PreviousTargetBackupPath is not null)
            {
                DeleteIfExists(journal.PreviousTargetBackupPath);
            }
            DeleteIfExists(journalPath);
            return;
        }

        throw new InvalidDataException("Migration recovery found neither the authenticated old nor new target.");
    }

    private static async ValueTask VerifyCompletedMigrationAsync(
        LegacyConfigurationMigrationJournal journal,
        CancellationToken cancellationToken)
    {
        await VerifySnapshotHashAsync(journal.TargetPath, journal.TargetSha256, cancellationToken).ConfigureAwait(false);
        await VerifySnapshotHashAsync(journal.ManifestPath, journal.ManifestSha256, cancellationToken).ConfigureAwait(false);
        await VerifySnapshotHashAsync(journal.ReportPath, journal.ReportSha256, cancellationToken).ConfigureAwait(false);
        await VerifySnapshotHashAsync(
            journal.SourceBackupPath, journal.SourceBackupSha256, cancellationToken).ConfigureAwait(false);
        if (journal.PreviousTargetBackupPath is not null && journal.PreviousTargetBackupSha256 is not null)
        {
            await VerifySnapshotHashAsync(
                journal.PreviousTargetBackupPath, journal.PreviousTargetBackupSha256, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async ValueTask CompleteRollbackAsync(
        LegacyConfigurationMigrationJournal journal,
        Action<MigrationFaultPoint>? faultInjector,
        CancellationToken cancellationToken)
    {
        await VerifySnapshotHashAsync(
            journal.SourceBackupPath, journal.SourceBackupSha256, cancellationToken).ConfigureAwait(false);
        byte[]? previousContent = null;
        if (journal.PreviousTargetBackupPath is not null && journal.PreviousTargetBackupSha256 is not null)
        {
            SecureFileSnapshot previous = await ReadOrdinaryFileAsync(
                journal.PreviousTargetBackupPath, MaximumSourceBytes, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(previous.Sha256, journal.PreviousTargetBackupSha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException("The previous target backup changed during rollback recovery.");
            }
            previousContent = previous.Content;
        }

        string? targetHash = File.Exists(journal.TargetPath)
            ? (await ReadOrdinaryFileAsync(journal.TargetPath, MaximumSourceBytes, cancellationToken)
                .ConfigureAwait(false)).Sha256
            : null;
        if (previousContent is null)
        {
            if (string.Equals(targetHash, journal.TargetSha256, StringComparison.Ordinal))
            {
                if (File.Exists(journal.PendingTargetPath))
                {
                    throw new InvalidDataException("Rollback recovery found an ambiguous pending target.");
                }
                CommitStagedFile(journal.TargetPath, journal.PendingTargetPath);
            }
            else if (targetHash is not null)
            {
                throw new InvalidDataException("Rollback recovery found an unauthenticated target.");
            }
        }
        else if (string.Equals(targetHash, journal.TargetSha256, StringComparison.Ordinal))
        {
            await WriteAtomicallyAsync(journal.TargetPath, previousContent, cancellationToken).ConfigureAwait(false);
        }
        else if (!string.Equals(targetHash, journal.PreviousTargetSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Rollback recovery found neither the authenticated old nor new target.");
        }
        faultInjector?.Invoke(MigrationFaultPoint.AfterRollbackTargetCommitted);

        await DeleteExpectedFileAsync(
            journal.ReportPath, journal.ReportSha256, cancellationToken).ConfigureAwait(false);
        await DeleteExpectedFileAsync(
            journal.PendingReportPath, journal.ReportSha256, cancellationToken).ConfigureAwait(false);
        faultInjector?.Invoke(MigrationFaultPoint.AfterRollbackReportRemoved);
        await DeleteExpectedFileAsync(
            journal.ManifestPath, journal.ManifestSha256, cancellationToken).ConfigureAwait(false);
        await DeleteExpectedFileAsync(
            journal.PendingManifestPath, journal.ManifestSha256, cancellationToken).ConfigureAwait(false);
        faultInjector?.Invoke(MigrationFaultPoint.AfterRollbackManifestRemoved);

        if (File.Exists(journal.PendingTargetPath))
        {
            await DeleteExpectedFileAsync(
                journal.PendingTargetPath, journal.TargetSha256, cancellationToken).ConfigureAwait(false);
        }
        DeleteIfExists(journal.TargetPath + ".migration-journal.json");
    }

    private static async ValueTask DeleteExpectedFileAsync(
        string path,
        string expectedHash,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return;
        }
        await VerifySnapshotHashAsync(path, expectedHash, cancellationToken).ConfigureAwait(false);
        DeleteIfExists(path);
    }

    private static async ValueTask PromoteOrVerifyAsync(
        string pendingPath,
        string finalPath,
        string expectedHash,
        CancellationToken cancellationToken)
    {
        if (File.Exists(finalPath))
        {
            await VerifySnapshotHashAsync(finalPath, expectedHash, cancellationToken).ConfigureAwait(false);
            await DeleteExpectedFileAsync(pendingPath, expectedHash, cancellationToken).ConfigureAwait(false);
            return;
        }

        await VerifySnapshotHashAsync(pendingPath, expectedHash, cancellationToken).ConfigureAwait(false);
        CommitStagedFile(pendingPath, finalPath);
        await VerifySnapshotHashAsync(finalPath, expectedHash, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask VerifySnapshotHashAsync(
        string path,
        string expectedHash,
        CancellationToken cancellationToken)
    {
        SecureFileSnapshot snapshot = await ReadOrdinaryFileAsync(path, MaximumSourceBytes, cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(snapshot.Sha256, expectedHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("A migration artefact does not match its authenticated hash.");
        }
    }

    private static LegacyConfigurationMigrationJournal DeserializeJournal(byte[] content)
    {
        try
        {
            return JsonSerializer.Deserialize<LegacyConfigurationMigrationJournal>(content, SerializerOptions)
                ?? throw new InvalidDataException("The migration journal is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The migration journal is invalid.", exception);
        }
    }

    private static void ValidateJournal(LegacyConfigurationMigrationJournal journal, string targetPath)
    {
        bool previousFieldsConsistent = (journal.PreviousTargetSha256 is null) ==
            (journal.PreviousTargetBackupPath is null) &&
            (journal.PreviousTargetBackupPath is null) == (journal.PreviousTargetBackupSha256 is null);
        if (journal.SchemaVersion != 1 || journal.State is not ("Prepared" or "Completed" or "RollingBack") ||
            !Path.IsPathFullyQualified(journal.SourcePath) || !PathsEqual(journal.TargetPath, targetPath) ||
            !PathsEqual(journal.ManifestPath, targetPath + ".migration-manifest.json") ||
            !PathsEqual(journal.ReportPath, targetPath + ".migration-report.json") ||
            !previousFieldsConsistent || !HasValidHash(journal.SourceSha256) ||
            !HasValidHash(journal.TargetSha256) || !HasValidHash(journal.SourceBackupSha256) ||
            !HasValidHash(journal.ManifestSha256) || !HasValidHash(journal.ReportSha256) ||
            (journal.PreviousTargetSha256 is not null && !HasValidHash(journal.PreviousTargetSha256)) ||
            (journal.PreviousTargetBackupSha256 is not null && !HasValidHash(journal.PreviousTargetBackupSha256)) ||
            !journal.SourceBackupPath.StartsWith(targetPath + ".source-", PathComparison()) ||
            (journal.PreviousTargetBackupPath is not null &&
                !journal.PreviousTargetBackupPath.StartsWith(targetPath + ".target-", PathComparison())))
        {
            throw new InvalidDataException("The migration journal escaped its exact target scope.");
        }

        ValidateDerivedPaths(targetPath, journal.SourceBackupPath, journal.PreviousTargetBackupPath,
            journal.ManifestPath, journal.ReportPath, targetPath + ".migration-journal.json",
            journal.PendingTargetPath, journal.PendingManifestPath, journal.PendingReportPath);
    }

    private static void ValidateDerivedPaths(string targetPath, params string?[] derivedPaths)
    {
        string targetDirectory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidDataException("The migration target directory is unavailable.");
        string targetName = Path.GetFileName(targetPath);
        HashSet<string> exactPaths = new(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(targetPath) };
        foreach (string? derivedPath in derivedPaths)
        {
            if (derivedPath is null)
            {
                continue;
            }

            string fullPath = Path.GetFullPath(derivedPath);
            if (!PathsEqual(Path.GetDirectoryName(fullPath)!, targetDirectory) ||
                !Path.GetFileName(fullPath).StartsWith(targetName + ".", StringComparison.Ordinal) ||
                !exactPaths.Add(fullPath))
            {
                throw new InvalidDataException("A migration artefact escaped or aliased the exact target scope.");
            }
        }
    }

    private static void EnsureDistinctPathAliases(string sourcePath, string targetPath)
    {
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Legacy source and migration target cannot be portable case aliases.");
        }
        string sourceDirectory = Path.GetDirectoryName(sourcePath)!;
        string targetDirectory = Path.GetDirectoryName(targetPath)!;
        EnsureNoReparseDirectories(sourceDirectory);
        if (Directory.Exists(targetDirectory))
        {
            EnsureNoReparseDirectories(targetDirectory);
        }

        if (File.Exists(targetPath))
        {
            SecureFileIdentity sourceIdentity = GetPathIdentity(sourcePath);
            SecureFileIdentity targetIdentity = GetPathIdentity(targetPath);
            if (sourceIdentity.SameFile(targetIdentity))
            {
                throw new InvalidDataException("Legacy source and migration target cannot alias the same file.");
            }
        }
    }

    private static async ValueTask<SecureFileSnapshot> ReadOrdinaryFileAsync(
        string path,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(path);
        FileInfo info = new(fullPath);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
            info.Length > maximumBytes)
        {
            throw new InvalidDataException("Migration input is unavailable or violates file policy.");
        }
        EnsureNoReparseDirectories(info.DirectoryName!);
        await using FileStream stream = new(
            fullPath, FileMode.Open, FileAccess.Read, FileShare.None, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        SecureFileIdentity before = GetHandleIdentity(stream.SafeFileHandle, stream.Length, info.LastWriteTimeUtc);
        if (before.LinkCount != 1)
        {
            throw new InvalidDataException("Migration files must have exactly one filesystem link.");
        }
        if (stream.Length > maximumBytes)
        {
            throw new InvalidDataException("A migration file exceeds the size policy.");
        }

        byte[] content = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(content, cancellationToken).ConfigureAwait(false);
        SecureFileIdentity after = GetHandleIdentity(stream.SafeFileHandle, stream.Length, info.LastWriteTimeUtc);
        if (!before.Equals(after))
        {
            throw new InvalidDataException("A migration file changed while it was being read.");
        }
        return new(fullPath, content, Hash(content), before);
    }

    private static async ValueTask RevalidateSnapshotAsync(
        SecureFileSnapshot expected,
        CancellationToken cancellationToken)
    {
        SecureFileSnapshot current = await ReadOrdinaryFileAsync(
            expected.Path, MaximumSourceBytes, cancellationToken).ConfigureAwait(false);
        if (!expected.Identity.SameFile(current.Identity) ||
            !string.Equals(expected.Sha256, current.Sha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("A migration input changed before commit or rollback.");
        }
    }

    private static async ValueTask RevalidateOptionalSnapshotAsync(
        string path,
        SecureFileSnapshot? expected,
        CancellationToken cancellationToken)
    {
        if (expected is null)
        {
            if (File.Exists(path))
            {
                throw new InvalidDataException("The migration target appeared concurrently before commit.");
            }
            return;
        }
        await RevalidateSnapshotAsync(expected, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask WriteNewDurablyAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken)
    {
        EnsureNoReparseDirectories(Path.GetDirectoryName(path)!);
        await using FileStream stream = new(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
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

    private static void CommitStagedFile(string pendingPath, string finalPath)
    {
        EnsureNoReparseDirectories(Path.GetDirectoryName(finalPath)!);
        if (File.Exists(finalPath))
        {
            _ = GetPathIdentity(finalPath);
        }
        File.Move(pendingPath, finalPath, overwrite: true);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            _ = GetPathIdentity(path);
            File.Delete(path);
        }
    }

    private static string Hash(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    private static bool HasValidHash(string? value) =>
        value?.Length == 64 && value.All(Uri.IsHexDigit);

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

    private static SecureFileIdentity GetPathIdentity(string path)
    {
        FileInfo info = new(path);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("The migration path is not an ordinary file.");
        }
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        SecureFileIdentity identity = GetHandleIdentity(stream.SafeFileHandle, stream.Length, info.LastWriteTimeUtc);
        if (identity.LinkCount != 1)
        {
            throw new InvalidDataException("Migration files must have exactly one filesystem link.");
        }
        return identity;
    }

    private static SecureFileIdentity GetHandleIdentity(
        SafeFileHandle handle,
        long length,
        DateTime lastWriteTimeUtc)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!GetFileInformationByHandle(handle, out ByHandleFileInformation information))
            {
                throw new IOException("The migration file identity could not be read.",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
            ulong fileId = ((ulong)information.FileIndexHigh << 32) | information.FileIndexLow;
            return new(information.VolumeSerialNumber, fileId, information.NumberOfLinks, length, lastWriteTimeUtc);
        }
        if (OperatingSystem.IsLinux())
        {
            if (FStat(handle, out LinuxStat information) != 0)
            {
                throw new IOException("The migration file identity could not be read.",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
            return new(information.Device, information.Inode, information.LinkCount, length, lastWriteTimeUtc);
        }
        throw new PlatformNotSupportedException("Secure migration file identity is supported only on Windows and Linux.");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle fileHandle,
        out ByHandleFileInformation fileInformation);

    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static extern int FStat(SafeFileHandle fileDescriptor, out LinuxStat buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LinuxStat
    {
        public ulong Device;
        public ulong Inode;
        public ulong LinkCount;
        public uint Mode;
        public uint UserId;
        public uint GroupId;
        public int Padding;
        public ulong RawDevice;
        public long Size;
        public long BlockSize;
        public long BlockCount;
        public long AccessSeconds;
        public long AccessNanoseconds;
        public long ModificationSeconds;
        public long ModificationNanoseconds;
        public long ChangeSeconds;
        public long ChangeNanoseconds;
    }

    private sealed record SecureFileSnapshot(
        string Path,
        byte[] Content,
        string Sha256,
        SecureFileIdentity Identity);

    private readonly record struct SecureFileIdentity(
        ulong Device,
        ulong FileId,
        ulong LinkCount,
        long Length,
        DateTime LastWriteTimeUtc)
    {
        public bool SameFile(SecureFileIdentity other) => Device == other.Device && FileId == other.FileId;
    }
}
