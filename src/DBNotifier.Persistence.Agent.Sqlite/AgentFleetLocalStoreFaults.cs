// Module purpose: Defines deterministic test-only fault points for Agent Fleet SQLite transactions without mutating host storage capacity.
namespace DBNotifier.Persistence.Agent.Sqlite;

/// <summary>Identifies bounded persistence boundaries where a sandbox test may inject one controlled failure.</summary>
public enum AgentFleetLocalStoreFaultPoint
{
    /// <summary>Immediately before an operation lease is acquired.</summary>
    BeforeLeaseAcquire,

    /// <summary>Immediately after an operation lease is durably acquired.</summary>
    AfterLeaseAcquire,

    /// <summary>Immediately before a newly created heartbeat envelope is committed.</summary>
    BeforeHeartbeatPendingCommit,

    /// <summary>Immediately after a newly created heartbeat envelope is committed.</summary>
    AfterHeartbeatPendingCommit,

    /// <summary>Immediately before a heartbeat acknowledgement is committed.</summary>
    BeforeHeartbeatAcknowledgementCommit,

    /// <summary>Immediately after a heartbeat acknowledgement is committed.</summary>
    AfterHeartbeatAcknowledgementCommit,

    /// <summary>Immediately before a complete assignment replacement is committed.</summary>
    BeforeAssignmentCommit,

    /// <summary>Immediately after a complete assignment replacement is committed.</summary>
    AfterAssignmentCommit,
}

/// <summary>Injects deterministic failures only when explicitly supplied by a local test fixture.</summary>
public interface IAgentFleetLocalStoreFaultInjector
{
    /// <summary>Throws or cancels at one named boundary, or returns without changing behaviour.</summary>
    /// <param name="point">Exact persistence boundary.</param>
    /// <param name="cancellationToken">Cancellation for the test hook.</param>
    ValueTask InjectAsync(AgentFleetLocalStoreFaultPoint point, CancellationToken cancellationToken);
}
