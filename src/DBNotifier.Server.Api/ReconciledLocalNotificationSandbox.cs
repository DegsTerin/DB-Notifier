// Module purpose: Projects committed synthetic transitions through one authenticated read-only HTTPS loopback sandbox endpoint.
using System.Text.Json;
using DBNotifier.Application.Presentation;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DBNotifier.Server.Api;

/// <summary>Registers and maps the isolated reconciled local-notification transition endpoint.</summary>
public static class ReconciledLocalNotificationSandboxEndpointExtensions
{
    /// <summary>Gets the explicit configuration section for this otherwise inert adapter.</summary>
    public const string ConfigurationSection = "ReconciledLocalNotificationSandbox";

    /// <summary>Gets the versioned read-only route available only in the Dashboard TV sandbox host.</summary>
    public const string TransitionRoute = "/api/v1/dashboard/reconciled-notification-transitions";

    /// <summary>Gets the response header that repeats the exact transition contract.</summary>
    public const string SchemaHeader = "DBN-Reconciled-Notification-Schema";

    /// <summary>Registers the projection only when both the existing sandbox and this increment are explicitly enabled.</summary>
    /// <param name="services">Sandbox service collection.</param>
    /// <param name="environment">Current host environment.</param>
    /// <param name="configuration">Current local configuration.</param>
    /// <returns>True only when mapping the endpoint is authorised for this process.</returns>
    public static bool AddReconciledLocalNotificationSandbox(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        bool enabled = environment.IsEnvironment(DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName) &&
            configuration.GetValue<bool>($"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled") &&
            configuration.GetValue<bool>($"{ConfigurationSection}:Enabled");
        if (enabled)
        {
            services.TryAddScoped<CommittedSyntheticTransitionSource>();
            services.TryAddScoped<IReconciledNotificationTransitionSource>(services =>
                services.GetRequiredService<CommittedSyntheticTransitionSource>());
        }

        return enabled;
    }

    /// <summary>Maps the bounded authenticated GET endpoint when registration proved both local guards.</summary>
    /// <param name="endpoints">Sandbox route builder.</param>
    /// <param name="enabled">Exact result returned by service registration.</param>
    /// <returns>The unchanged route builder.</returns>
    public static IEndpointRouteBuilder MapReconciledLocalNotificationSandbox(
        this IEndpointRouteBuilder endpoints,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        if (!enabled)
        {
            return endpoints;
        }

        endpoints.MapGet(TransitionRoute, ReadAsync)
            .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");
        return endpoints;
    }

    private static async Task<IResult> ReadAsync(
        HttpContext context,
        IReconciledNotificationTransitionSource source,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        bool hasBaseline = context.Request.Query.TryGetValue("baseline", out var baselineValues);
        bool hasCursor = context.Request.Query.TryGetValue("cursor", out var cursorValues);
        bool baseline = hasBaseline && baselineValues.Count == 1 &&
            string.Equals(baselineValues[0], bool.TrueString, StringComparison.OrdinalIgnoreCase);
        string? cursor = hasCursor && cursorValues.Count == 1 ? cursorValues[0] : null;
        if (context.Request.Query.Count != 1 || baseline == hasCursor ||
            (!baseline && (string.IsNullOrWhiteSpace(cursor) ||
                !ReconciledNotificationCursorCodec.TryDecode(cursor, out _))))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid reconciled notification query",
                extensions: new Dictionary<string, object?> { ["code"] = "reconciled_notification.query_invalid" });
        }

        ReconciledNotificationTransitionPage page;
        try
        {
            page = await source.ReadAsync(
                new ReconciledNotificationTransitionQuery(baseline, cursor),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Reconciled notification projection unavailable",
                extensions: new Dictionary<string, object?> { ["code"] = "reconciled_notification.projection_unavailable" });
        }

        string requested = baseline ? ReconciledNotificationTransitionContract.EmptyCursor : cursor!;
        if (!ReconciledNotificationTransitionValidator.TryValidate(
                page,
                requested,
                baseline,
                timeProvider.GetUtcNow(),
                out string errorCode))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Reconciled notification projection invalid",
                extensions: new Dictionary<string, object?> { ["code"] = errorCode });
        }

        context.Response.Headers[SchemaHeader] = ReconciledNotificationTransitionContract.CurrentSchemaVersion;
        context.Response.Headers.CacheControl = "private, no-store";
        return Results.Bytes(JsonSerializer.SerializeToUtf8Bytes(page, JsonSerializerOptions.Web), "application/json; charset=utf-8");
    }
}

/// <summary>Projects exact committed events and their synthetic source observations within a fixed resource envelope.</summary>
public sealed class CommittedSyntheticTransitionSource : IReconciledNotificationTransitionSource
{
    private const int MaximumCandidateCount =
        (ReconciledNotificationTransitionContract.MaximumItemCount + 1) *
        ReconciledNotificationTransitionContract.MaximumCursorAgents;
    private readonly IDbContextFactory<ServerDbContext> contextFactory;
    private readonly TimeProvider timeProvider;

    /// <summary>Initialises the projection from an explicit context factory and trusted clock.</summary>
    /// <param name="contextFactory">Factory for short-lived read-only server contexts.</param>
    /// <param name="timeProvider">Trusted clock used only for freshness classification.</param>
    public CommittedSyntheticTransitionSource(
        IDbContextFactory<ServerDbContext> contextFactory,
        TimeProvider timeProvider)
    {
        this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async ValueTask<ReconciledNotificationTransitionPage> ReadAsync(
        ReconciledNotificationTransitionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using ServerDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, long> heads = await context.AgentObservationCursors
            .AsNoTracking()
            .OrderBy(row => row.AgentId)
            .Take(ReconciledNotificationTransitionContract.MaximumCursorAgents + 1)
            .ToDictionaryAsync(row => row.AgentId, row => row.HighestContiguousSequence, cancellationToken)
            .ConfigureAwait(false);
        if (heads.Count > ReconciledNotificationTransitionContract.MaximumCursorAgents)
        {
            throw new InvalidOperationException("The synthetic sandbox exceeds the bounded Agent cursor envelope.");
        }

        if (query.Baseline)
        {
            if (query.Cursor is not null)
            {
                throw new InvalidOperationException("A baseline cannot carry a continuation cursor.");
            }
            return new(
                ReconciledNotificationTransitionContract.CurrentSchemaVersion,
                ReconciledNotificationTransitionContract.EmptyCursor,
                ReconciledNotificationCursorCodec.Encode(heads),
                false,
                []);
        }

        if (!ReconciledNotificationCursorCodec.TryDecode(query.Cursor, out IReadOnlyDictionary<Guid, long>? previous) ||
            previous.Any(item => !heads.TryGetValue(item.Key, out long head) || item.Value > head))
        {
            throw new InvalidOperationException("The continuation cursor is not compatible with current committed heads.");
        }

        Dictionary<Guid, long> from = heads.Keys.ToDictionary(
            agentId => agentId,
            agentId => previous.TryGetValue(agentId, out long sequence) ? sequence : 0L);
        List<ProjectionCandidate> candidates = [];
        foreach ((Guid agentId, long head) in heads.OrderBy(item => item.Key))
        {
            long after = from[agentId];
            if (after == head)
            {
                continue;
            }

            List<ProjectionCandidate> agentCandidates = await (
                from eventRow in context.Events.AsNoTracking()
                join sample in context.HealthSamples.AsNoTracking()
                    on eventRow.SourceObservationId equals (Guid?)sample.ObservationId
                join instance in context.Instances.AsNoTracking()
                    on eventRow.InstanceId equals (Guid?)instance.InstanceId
                where eventRow.AgentId == agentId && sample.AgentId == agentId &&
                    sample.Sequence > after && sample.Sequence <= head
                orderby sample.Sequence
                select new ProjectionCandidate(
                    eventRow.EventId,
                    instance.InstanceId,
                    instance.DisplayName,
                    eventRow.EventType,
                    eventRow.Severity,
                    eventRow.ObservedAt,
                    eventRow.ReceivedAt,
                    eventRow.DetailsJson,
                    sample.EvidenceLevel,
                    sample.Status,
                    sample.ObservedAt,
                    sample.ReceivedAt,
                    sample.Sequence,
                    agentId))
                .Take(ReconciledNotificationTransitionContract.MaximumItemCount + 1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            candidates.AddRange(agentCandidates);
            if (candidates.Count > MaximumCandidateCount)
            {
                throw new InvalidOperationException("The synthetic transition projection exceeded its materialisation budget.");
            }
        }

        ProjectionCandidate[] ordered = candidates
            .OrderBy(item => item.AgentId)
            .ThenBy(item => item.Sequence)
            .ThenBy(item => item.EventId)
            .ToArray();
        ProjectionCandidate[] selected = ordered
            .Take(ReconciledNotificationTransitionContract.MaximumItemCount)
            .ToArray();
        Dictionary<Guid, long> next = new(from);
        foreach ((Guid agentId, long head) in heads)
        {
            long? maximumSelected = selected
                .Where(item => item.AgentId == agentId)
                .Select(item => (long?)item.Sequence)
                .Max();
            bool hasUnselected = ordered.Any(item => item.AgentId == agentId &&
                (maximumSelected is null || item.Sequence > maximumSelected));
            next[agentId] = hasUnselected ? maximumSelected ?? from[agentId] : head;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        ReconciledNotificationTransition[] transitions = selected
            .Select(item => ToTransition(item, now))
            .Where(item => item is not null)
            .Cast<ReconciledNotificationTransition>()
            .ToArray();
        return new(
            ReconciledNotificationTransitionContract.CurrentSchemaVersion,
            query.Cursor!,
            ReconciledNotificationCursorCodec.Encode(next),
            next.Any(item => item.Value < heads[item.Key]),
            transitions);
    }

    private static ReconciledNotificationTransition? ToTransition(ProjectionCandidate candidate, DateTimeOffset now)
    {
        if (!string.Equals(candidate.EvidenceLevel, "Synthetic", StringComparison.Ordinal))
        {
            return null;
        }
        if (!TryReadStatuses(candidate.DetailsJson, out string? previous, out string? current))
        {
            throw new InvalidOperationException("A committed synthetic event has incompatible transition details.");
        }
        if (!string.Equals(current, candidate.SampleStatus, StringComparison.Ordinal) ||
            candidate.ObservedAt != candidate.SampleObservedAt ||
            candidate.ReceivedAt != candidate.SampleReceivedAt)
        {
            throw new InvalidOperationException("A committed synthetic event conflicts with its source observation.");
        }
        if (previous is null)
        {
            if (!string.Equals(candidate.EventType, "Connected", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A committed synthetic transition has no factual previous status.");
            }
            return null;
        }

        string freshness = candidate.ReceivedAt > now
            ? "unknown"
            : now - candidate.ReceivedAt <= ReconciledNotificationTransitionContract.CurrentEvidenceLifetime
                ? "current"
                : "stale";
        return new(
            candidate.EventId,
            candidate.InstanceId,
            candidate.DisplayName,
            candidate.EventType,
            candidate.Severity,
            previous,
            current!,
            candidate.ObservedAt,
            candidate.ReceivedAt,
            freshness,
            ReconciledNotificationTransitionContract.SyntheticSourceKind);
    }

    private static bool TryReadStatuses(string detailsJson, out string? previous, out string? current)
    {
        previous = null;
        current = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(detailsJson, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 4,
            });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 ||
                !root.TryGetProperty("schemaVersion", out JsonElement schema) ||
                !string.Equals(schema.GetString(), "canonical-event-details.v1", StringComparison.Ordinal) ||
                !root.TryGetProperty("previousStatus", out JsonElement previousElement) ||
                !root.TryGetProperty("currentStatus", out JsonElement currentElement) ||
                currentElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }
            previous = previousElement.ValueKind == JsonValueKind.Null ? null : previousElement.GetString();
            current = currentElement.GetString();
            return previousElement.ValueKind is JsonValueKind.Null or JsonValueKind.String && current is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record ProjectionCandidate(
        Guid EventId,
        Guid InstanceId,
        string DisplayName,
        string EventType,
        string Severity,
        DateTimeOffset ObservedAt,
        DateTimeOffset ReceivedAt,
        string DetailsJson,
        string EvidenceLevel,
        string SampleStatus,
        DateTimeOffset SampleObservedAt,
        DateTimeOffset SampleReceivedAt,
        long Sequence,
        Guid AgentId);
}
