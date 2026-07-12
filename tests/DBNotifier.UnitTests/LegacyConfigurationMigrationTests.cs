// Module purpose: Verifies Legacy Configuration Migration Tests behaviour and protects the documented project contract.
using System.Security.Cryptography;
using System.Text.Json;
using DBNotifier.ConfigMigrator;

namespace DBNotifier.UnitTests;

public sealed class LegacyConfigurationMigrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 21, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DryRunMapsLegacyFieldsWithoutWritingOrChangingSource()
    {
        using MigrationFixture fixture = await MigrationFixture.CreateAsync(ValidConfiguration());
        string sourceHash = await FileHashAsync(fixture.SourcePath);
        LegacyConfigurationMigrator migrator = new(new FixedTimeProvider(Now));

        LegacyConfigurationMigrationResult result = await migrator.MigrateAsync(
            new(fixture.SourcePath, fixture.TargetPath, ApplyChanges: false));

        Assert.Equal("DryRun", result.Report.Status);
        Assert.False(result.Report.Applied);
        Assert.False(File.Exists(fixture.TargetPath));
        Assert.Equal(sourceHash, await FileHashAsync(fixture.SourcePath));
        LegacyConfigurationTarget target = Assert.IsType<LegacyConfigurationTarget>(result.Target);
        ImportedLegacyInstance instance = Assert.Single(target.Instances);
        Assert.Equal("postgresql", instance.ProviderType);
        Assert.Equal("db.example.test", instance.Host);
        Assert.Equal(5544, instance.Port);
        Assert.True(instance.AdministrativeControl.Requested);
        Assert.Equal("ManualActionRequired", instance.AdministrativeControl.State);
        Assert.Equal("Degraded", target.MonitoringPolicy.TcpFallbackEvidence);
        Assert.Equal("pg_isready.exe", target.MonitoringPolicy.PgIsReadyPath);
    }

    [Fact]
    public async Task ApplyBacksUpWritesAtomicallyIsIdempotentAndRollsBack()
    {
        using MigrationFixture fixture = await MigrationFixture.CreateAsync(ValidConfiguration());
        string sourceHash = await FileHashAsync(fixture.SourcePath);
        LegacyConfigurationMigrator migrator = new(new FixedTimeProvider(Now));

        LegacyConfigurationMigrationResult applied = await migrator.MigrateAsync(
            new(fixture.SourcePath, fixture.TargetPath, ApplyChanges: true));
        LegacyConfigurationMigrationResult rerun = await migrator.MigrateAsync(
            new(fixture.SourcePath, fixture.TargetPath, ApplyChanges: true));

        Assert.Equal("Applied", applied.Report.Status);
        Assert.True(File.Exists(fixture.TargetPath));
        Assert.True(File.Exists(applied.Report.SourceBackupPath));
        Assert.True(File.Exists(applied.Report.ManifestPath));
        Assert.Equal("AlreadyCurrent", rerun.Report.Status);
        Assert.Single(Directory.GetFiles(fixture.DirectoryPath, "*.source-*.bak"));
        Assert.Equal(sourceHash, await FileHashAsync(fixture.SourcePath));
        Assert.True(await LegacyConfigurationMigrator.RollbackAsync(applied.Report.ManifestPath!));
        Assert.False(File.Exists(fixture.TargetPath));
        Assert.False(File.Exists(applied.Report.ManifestPath));
        Assert.True(File.Exists(applied.Report.SourceBackupPath));
    }

    [Fact]
    public async Task SecretAndUnsupportedFieldsRejectWithoutCopyingValueOrWriting()
    {
        const string canary = "migration-secret-canary";
        using MigrationFixture fixture = await MigrationFixture.CreateAsync(
            $$"""{"instances":[{"name":"db","hostName":"localhost","port":5432,"password":"{{canary}}"}]}""");
        LegacyConfigurationMigrator migrator = new(new FixedTimeProvider(Now));

        LegacyConfigurationMigrationResult result = await migrator.MigrateAsync(
            new(fixture.SourcePath, fixture.TargetPath, ApplyChanges: true));
        string reportJson = JsonSerializer.Serialize(result.Report);

        Assert.Equal("Rejected", result.Report.Status);
        Assert.Contains(result.Report.Items, item => item.Code == "migration.secret_blocked");
        Assert.DoesNotContain(canary, reportJson, StringComparison.Ordinal);
        Assert.False(File.Exists(fixture.TargetPath));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.bak"));
    }

    [Theory]
    [InlineData("{\"instances\":[{\"name\":\"a\",\"hostName\":\"db\",\"port\":5432},{\"name\":\"b\",\"hostName\":\"db\",\"port\":5432}]}", "migration.instance_identity_invalid")]
    [InlineData("{\"pgIsReady\":{\"timeoutSeconds\":0},\"instances\":[]}", "migration.number_out_of_range")]
    [InlineData("{\"pgIsReady\":{\"extraArguments\":[\"--unsafe\"]},\"instances\":[]}", "migration.extra_arguments_unsupported")]
    [InlineData("{\"unknownSection\":{},\"instances\":[]}", "migration.field_unsupported")]
    public async Task InvalidLegacyIntentIsRejected(string json, string expectedCode)
    {
        using MigrationFixture fixture = await MigrationFixture.CreateAsync(json);
        LegacyConfigurationMigrator migrator = new(new FixedTimeProvider(Now));

        LegacyConfigurationMigrationResult result = await migrator.MigrateAsync(
            new(fixture.SourcePath, fixture.TargetPath, ApplyChanges: false));

        Assert.Equal("Rejected", result.Report.Status);
        Assert.Contains(result.Report.Items, item => item.Code == expectedCode);
    }

    [Fact]
    public async Task RollbackRefusesTargetChangedAfterMigration()
    {
        using MigrationFixture fixture = await MigrationFixture.CreateAsync(ValidConfiguration());
        LegacyConfigurationMigrator migrator = new(new FixedTimeProvider(Now));
        LegacyConfigurationMigrationResult applied = await migrator.MigrateAsync(
            new(fixture.SourcePath, fixture.TargetPath, ApplyChanges: true));
        await File.WriteAllTextAsync(fixture.TargetPath, "{\"changed\":true}");

        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await LegacyConfigurationMigrator.RollbackAsync(applied.Report.ManifestPath!));
        Assert.True(File.Exists(fixture.TargetPath));
    }

    [Fact]
    public async Task RollbackRestoresPreviousTargetFromItsOwnBackup()
    {
        using MigrationFixture fixture = await MigrationFixture.CreateAsync(ValidConfiguration());
        const string previousTarget = "{\"schemaVersion\":0}";
        await File.WriteAllTextAsync(fixture.TargetPath, previousTarget);
        LegacyConfigurationMigrator migrator = new(new FixedTimeProvider(Now));

        LegacyConfigurationMigrationResult applied = await migrator.MigrateAsync(
            new(fixture.SourcePath, fixture.TargetPath, ApplyChanges: true));
        Assert.True(File.Exists(applied.Report.PreviousTargetBackupPath));

        Assert.True(await LegacyConfigurationMigrator.RollbackAsync(applied.Report.ManifestPath!));
        Assert.Equal(previousTarget, await File.ReadAllTextAsync(fixture.TargetPath));
    }

    private static string ValidConfiguration() =>
        """
        {
          "application": { "intervalSeconds": 15 },
          "notifications": { "enabled": true },
          "pgIsReady": { "path": "pg_isready.exe", "timeoutSeconds": 7, "retryCount": 2, "retryDelayMs": 1000, "extraArguments": [] },
          "instances": [
            {
              "name": "Reporting",
              "serviceName": "postgresql-x64-18",
              "hostName": "db.example.test",
              "port": 5544,
              "postgresExe": "C:\\Program Files\\PostgreSQL\\18\\bin\\postgres.exe",
              "enabled": true,
              "notificationsEnabled": false,
              "restartAllowed": true
            }
          ]
        }
        """;

    private static async Task<string> FileHashAsync(string path) =>
        Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path)));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class MigrationFixture(string directoryPath) : IDisposable
    {
        public string DirectoryPath { get; } = directoryPath;
        public string SourcePath { get; } = Path.Combine(directoryPath, "pgnotifier.json");
        public string TargetPath { get; } = Path.Combine(directoryPath, "dbnotifier.json");

        public static async Task<MigrationFixture> CreateAsync(string json)
        {
            MigrationFixture fixture = new(Path.Combine(Path.GetTempPath(), $"dbnotifier-migration-{Guid.NewGuid():N}"));
            Directory.CreateDirectory(fixture.DirectoryPath);
            await File.WriteAllTextAsync(fixture.SourcePath, json);
            return fixture;
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
