// Module purpose: Records bounded authentication outcomes without persisting tokens, certificates or exception details.
using System.Text.Json;
using System.Threading.RateLimiting;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Server.Api.Security;

/// <summary>
/// Provides a bounded, cardinality-constant admission gate for authentication audit persistence.
/// </summary>
public interface IAuthenticationAuditGate
{
    /// <summary>
    /// Attempts to reserve capacity for one sanitised authentication audit event.
    /// </summary>
    /// <returns>The bounded persistence and suppression-reporting decision.</returns>
    AuthenticationAuditAdmission Acquire();
}

/// <summary>Describes whether an authentication event may persist or should emit one aggregated suppression warning.</summary>
public enum AuthenticationAuditAdmission
{
    /// <summary>The event may reach the append-only audit store.</summary>
    Accepted,

    /// <summary>The event is suppressed and owns the single warning for the current minute.</summary>
    SuppressedAndReport,

    /// <summary>The event is suppressed without another warning in the current minute.</summary>
    Suppressed,
}

/// <summary>
/// Limits all authentication audit persistence through one fixed window, avoiding attacker-controlled
/// partition dictionaries while bounding database work before endpoint rate limiting can run.
/// </summary>
public sealed class AuthenticationAuditGate : IAuthenticationAuditGate, IDisposable
{
    private const int DefaultPermitLimit = 120;
    private readonly FixedWindowRateLimiter limiter;
    private readonly TimeProvider timeProvider;
    private long lastReportedSuppressionWindow = long.MinValue;

    /// <summary>
    /// Creates the production gate with the bounded default permit limit.
    /// </summary>
    public AuthenticationAuditGate()
        : this(DefaultPermitLimit, TimeProvider.System)
    {
    }

    /// <summary>
    /// Creates a gate with an explicit bounded permit limit, primarily for deterministic validation.
    /// </summary>
    /// <param name="permitLimit">Maximum events admitted in one minute, between one and 1,000.</param>
    public AuthenticationAuditGate(int permitLimit)
        : this(permitLimit, TimeProvider.System)
    {
    }

    /// <summary>Creates a deterministic gate with an explicit clock for suppression-window validation.</summary>
    /// <param name="permitLimit">Maximum events admitted in one minute, between one and 1,000.</param>
    /// <param name="timeProvider">Clock used only to aggregate suppression warnings by UTC minute.</param>
    public AuthenticationAuditGate(int permitLimit, TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(permitLimit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(permitLimit, 1000);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.timeProvider = timeProvider;
        limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    /// <inheritdoc />
    public AuthenticationAuditAdmission Acquire()
    {
        using RateLimitLease lease = limiter.AttemptAcquire(1);
        if (lease.IsAcquired)
        {
            return AuthenticationAuditAdmission.Accepted;
        }

        long currentWindow = timeProvider.GetUtcNow().ToUnixTimeSeconds() / 60;
        while (true)
        {
            long previous = Volatile.Read(ref lastReportedSuppressionWindow);
            if (previous == currentWindow)
            {
                return AuthenticationAuditAdmission.Suppressed;
            }

            if (Interlocked.CompareExchange(
                ref lastReportedSuppressionWindow,
                currentWindow,
                previous) == previous)
            {
                return AuthenticationAuditAdmission.SuppressedAndReport;
            }
        }
    }

    /// <summary>
    /// Releases limiter resources when the host shuts down.
    /// </summary>
    public void Dispose() => limiter.Dispose();
}

/// <summary>
/// Describes a sanitised authentication outcome suitable for the append-only central audit store.
/// </summary>
/// <param name="ActorType">The bounded canonical actor category.</param>
/// <param name="ActorId">The subject or Agent identifier, or <c>unresolved</c> when authentication failed early.</param>
/// <param name="Scheme">The bounded authentication scheme identifier.</param>
/// <param name="Outcome">The canonical audit outcome.</param>
/// <param name="Code">A stable non-secret reason code.</param>
public sealed record AuthenticationAuditEvent(
    string ActorType,
    string ActorId,
    string Scheme,
    string Outcome,
    string Code);

/// <summary>
/// Writes authentication evidence on a best-effort basis so an unavailable audit database cannot disclose credentials
/// or replace the authentication result with a persistence exception.
/// </summary>
/// <param name="contextFactory">Factory for isolated central audit persistence contexts.</param>
/// <param name="timeProvider">Clock used for append-only audit timestamps.</param>
/// <param name="gate">Global bounded admission gate applied before persistence work.</param>
/// <param name="logger">Structured logger that receives stable non-secret codes only.</param>
public sealed partial class AuthenticationAuditWriter(
    IDbContextFactory<ServerDbContext> contextFactory,
    TimeProvider timeProvider,
    IAuthenticationAuditGate gate,
    ILogger<AuthenticationAuditWriter> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedOutcomes = new(StringComparer.Ordinal)
    {
        "Succeeded",
        "Failed",
        "Denied",
        "Unknown",
    };

    /// <summary>
    /// Persists one bounded authentication outcome without throwing ordinary persistence failures to the caller.
    /// </summary>
    /// <param name="auditEvent">The sanitised event containing no token, certificate or exception material.</param>
    /// <param name="cancellationToken">Cancellation propagated from the current request.</param>
    /// <returns>A task that completes after the write attempt.</returns>
    public async ValueTask TryWriteAsync(
        AuthenticationAuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        Validate(auditEvent);
        AuthenticationAuditAdmission admission = gate.Acquire();
        if (admission != AuthenticationAuditAdmission.Accepted)
        {
            if (admission == AuthenticationAuditAdmission.SuppressedAndReport)
            {
                LogAuditRateLimited(logger);
            }
            return;
        }

        try
        {
            await using ServerDbContext context = await contextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            context.AuditEntries.Add(new AuditEntryRow
            {
                AuditEntryId = Guid.NewGuid(),
                OccurredAt = timeProvider.GetUtcNow(),
                ActorType = auditEvent.ActorType,
                ActorId = auditEvent.ActorId,
                Action = "authentication.validate",
                TargetType = "AuthenticationScheme",
                TargetId = auditEvent.Scheme,
                Outcome = auditEvent.Outcome,
                CorrelationId = Guid.NewGuid(),
                DetailsJson = JsonSerializer.Serialize(new { code = auditEvent.Code }, SerializerOptions),
            });
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            LogAuditUnavailable(logger, auditEvent.Code);
        }
    }

    private static void Validate(AuthenticationAuditEvent auditEvent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.ActorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.Scheme);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.Code);
        if (auditEvent.ActorType.Length > 64 || auditEvent.ActorId.Length > 300 ||
            auditEvent.Scheme.Length > 100 || auditEvent.Code.Length > 100 ||
            !AllowedOutcomes.Contains(auditEvent.Outcome))
        {
            throw new ArgumentException("Authentication audit evidence is outside policy.", nameof(auditEvent));
        }
    }

    [LoggerMessage(
        EventId = 3201,
        Level = LogLevel.Warning,
        Message = "Authentication audit persistence is unavailable: code={Code}")]
    private static partial void LogAuditUnavailable(ILogger logger, string code);

    [LoggerMessage(
        EventId = 3202,
        Level = LogLevel.Warning,
        Message = "Authentication audit persistence was suppressed by the bounded rate limit")]
    private static partial void LogAuditRateLimited(ILogger logger);
}
