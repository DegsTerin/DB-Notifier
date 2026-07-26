// Module purpose: Exposes bounded, sanitised Server readiness independently from process liveness.
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DBNotifier.Server.Api;

/// <summary>Represents one sanitised central persistence readiness result.</summary>
/// <param name="Connected">Whether the configured central database accepted a bounded connection.</param>
/// <param name="SchemaCompatible">Whether applied migrations exactly match the compiled Server schema.</param>
public sealed record ServerReadinessDatabaseResult(bool Connected, bool SchemaCompatible);

/// <summary>Checks the central persistence dependency without exposing connection or provider diagnostics.</summary>
public interface IServerReadinessDatabase
{
    /// <summary>Checks bounded connectivity and exact compiled migration compatibility.</summary>
    /// <param name="cancellationToken">Token enforcing the caller's readiness deadline.</param>
    /// <returns>Sanitised connectivity and schema compatibility facts.</returns>
    ValueTask<ServerReadinessDatabaseResult> CheckAsync(CancellationToken cancellationToken);
}

/// <summary>Uses the normal Server context factory to assess central PostgreSQL and migration compatibility.</summary>
/// <param name="contextFactory">Normal central persistence context factory.</param>
public sealed class EfServerReadinessDatabase(IDbContextFactory<ServerDbContext> contextFactory)
    : IServerReadinessDatabase
{
    /// <inheritdoc />
    public async ValueTask<ServerReadinessDatabaseResult> CheckAsync(CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(
                context.Database.ProviderName,
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                StringComparison.Ordinal))
        {
            return new ServerReadinessDatabaseResult(false, false);
        }

        if (!await context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            return new ServerReadinessDatabaseResult(false, false);
        }

        string[] compiled = context.Database.GetMigrations().ToArray();
        string[] applied = (await context.Database
                .GetAppliedMigrationsAsync(cancellationToken)
                .ConfigureAwait(false))
            .ToArray();
        bool schemaCompatible = compiled.Length > 0 && compiled.SequenceEqual(applied, StringComparer.Ordinal);
        return new ServerReadinessDatabaseResult(true, schemaCompatible);
    }
}

/// <summary>Represents one endpoint-safe readiness decision.</summary>
/// <param name="IsReady">Whether the Server may receive operational traffic.</param>
/// <param name="Code">Stable non-secret reason code when the Server is not ready.</param>
public sealed record ServerReadinessResult(bool IsReady, string? Code);

/// <summary>
/// Validates non-secret central database configuration, connectivity and schema under one hard readiness deadline.
/// </summary>
/// <param name="configuration">Fallback configuration used only by isolated callers without the production factory.</param>
/// <param name="database">Central persistence readiness boundary.</param>
/// <param name="databaseConfiguration">
/// Production network-bound configuration state; null retains the isolated compatibility seam.
/// </param>
public sealed class ServerReadinessProbe(
    IConfiguration configuration,
    IServerReadinessDatabase database,
    IServerDatabaseConfigurationReadiness? databaseConfiguration = null)
{
    private static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(5);

    /// <summary>Evaluates configuration, central PostgreSQL connectivity and schema compatibility.</summary>
    /// <param name="cancellationToken">Request cancellation, distinct from the internal hard deadline.</param>
    /// <returns>A ready result or one stable sanitised failure code.</returns>
    /// <exception cref="OperationCanceledException">Thrown only when the request caller cancels the check.</exception>
    public async ValueTask<ServerReadinessResult> CheckAsync(CancellationToken cancellationToken)
    {
        bool configurationIsValid = databaseConfiguration?.IsConfiguredAndValid ??
            HasValidNonSecretShape(configuration.GetConnectionString("ServerDatabase"));
        if (!configurationIsValid)
        {
            return new ServerReadinessResult(false, "server.readiness.configuration_invalid");
        }

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(MaximumDuration);
        try
        {
            ServerReadinessDatabaseResult result = await database
                .CheckAsync(deadline.Token)
                .AsTask()
                .WaitAsync(deadline.Token)
                .ConfigureAwait(false);
            if (!result.Connected)
            {
                return new ServerReadinessResult(false, "server.readiness.database_unavailable");
            }

            return result.SchemaCompatible
                ? new ServerReadinessResult(true, null)
                : new ServerReadinessResult(false, "server.readiness.schema_incompatible");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return new ServerReadinessResult(false, "server.readiness.deadline_exceeded");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return new ServerReadinessResult(false, "server.readiness.database_unavailable");
        }
    }

    /// <summary>Checks syntax and required non-secret keys without retaining or returning any configured value.</summary>
    private static bool HasValidNonSecretShape(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Length > 4096)
        {
            return false;
        }

        try
        {
            NpgsqlConnectionStringBuilder builder = new() { ConnectionString = connectionString };
            return !string.IsNullOrWhiteSpace(builder.Host) && !string.IsNullOrWhiteSpace(builder.Database);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

/// <summary>Maps separate public liveness and dependency-aware readiness endpoints.</summary>
public static class ServerHealthEndpointRouteBuilderExtensions
{
    /// <summary>Maps process liveness and bounded sanitised operational readiness endpoints.</summary>
    /// <param name="endpoints">Endpoint route builder owned by the Server host.</param>
    /// <returns>The same route builder for composition.</returns>
    public static IEndpointRouteBuilder MapServerHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/health/live", static () => Results.Ok(new { status = "Alive" }));
        endpoints.MapGet(
            "/health/ready",
            async Task<IResult> (ServerReadinessProbe probe, HttpContext context, CancellationToken cancellationToken) =>
            {
                ServerReadinessResult result = await probe.CheckAsync(cancellationToken).ConfigureAwait(false);
                context.Response.Headers.CacheControl = "no-store";
                return result.IsReady
                    ? Results.Ok(new { status = "Ready" })
                    : Results.Json(
                        new { status = "NotReady", code = result.Code },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
            });
        return endpoints;
    }
}
