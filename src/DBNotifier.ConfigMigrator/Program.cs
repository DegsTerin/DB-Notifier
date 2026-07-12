// Module purpose: Implements Program for isolated, fail-closed legacy configuration migration.
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.ConfigMigrator;

JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);
serializerOptions.Converters.Add(new JsonStringEnumConverter());
return await RunAsync(args, serializerOptions).ConfigureAwait(false);

static async Task<int> RunAsync(string[] args, JsonSerializerOptions serializerOptions)
{
    try
    {
        if (args.Length == 2 && string.Equals(args[0], "rollback", StringComparison.Ordinal))
        {
            _ = await LegacyConfigurationMigrator.RollbackAsync(args[1]).ConfigureAwait(false);
            Console.WriteLine("{\"status\":\"RolledBack\"}");
            return 0;
        }

        if (args.Length is < 5 or > 6 || !string.Equals(args[0], "migrate", StringComparison.Ordinal) ||
            !string.Equals(args[1], "--source", StringComparison.Ordinal) ||
            !string.Equals(args[3], "--target", StringComparison.Ordinal) ||
            (args.Length == 6 && !string.Equals(args[5], "--apply", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                "Usage: DBNotifier.ConfigMigrator migrate --source <absolute-json> --target <absolute-json> [--apply] | rollback <absolute-manifest>");
            return 2;
        }

        LegacyConfigurationMigrator migrator = new(TimeProvider.System);
        LegacyConfigurationMigrationResult result = await migrator.MigrateAsync(
            new(args[2], args[4], ApplyChanges: args.Length == 6)).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(result.Report, serializerOptions));
        return string.Equals(result.Report.Status, "Rejected", StringComparison.Ordinal) ? 3 : 0;
    }
    catch (Exception exception) when (exception is not OutOfMemoryException)
    {
        Console.Error.WriteLine("Configuration migration failed safely.");
        return 4;
    }
}
