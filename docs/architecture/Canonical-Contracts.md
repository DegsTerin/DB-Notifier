# Canonical Architecture Contracts

## Status

Normative contracts accepted in `STATE-02`. `STATE-03` implemented their persistence subset, and the first `STATE-04` increment implements the provider type, health, error, credential-reference, capability, endpoint, and probe subset. Unlisted portions remain contractual targets rather than implementation claims.

## Common envelope

Every durable message uses:

```text
MessageEnvelope<T>
  messageId: UUID
  schemaVersion: positive integer
  agentId: UUID when Agent-originated
  sequence: non-negative integer when Agent-originated
  correlationId: UUID
  causationId: UUID optional
  occurredAt: UTC instant
  sentAt: UTC instant
  payload: T
```

IDs are generated once and survive retries. `occurredAt` describes source observation; server `receivedAt` is assigned on ingestion and never substituted for it. Unknown clock quality is explicit.

## Instance identity and endpoint

```text
InstanceDescriptor
  instanceId: UUID
  displayName: string
  providerType: stable lowercase identifier
  environment: string
  tags: map<string,string>
  endpoint: ProviderEndpoint
  monitoringCredentialRef: opaque reference optional
  administrativeCredentialRef: opaque reference optional
  policyId: UUID
  assignedAgentId: UUID
  enabled: boolean
  version: concurrency token
```

`ProviderEndpoint` is a tagged provider-owned object validated against a registered schema. Common host/port fields may be indexed, but the core does not force every engine into a PostgreSQL connection model. Secret values are forbidden.

## Health observation

```text
HealthObservation
  observationId: UUID
  instanceId: UUID
  agentId: UUID
  providerType: string
  providerVersion: string
  status: HealthStatus
  method: ProbeMethod
  observedAt: UTC instant
  durationMs: non-negative integer
  clockSkewMs: integer optional
  quality: ObservationQuality
  error: NormalizedError optional
  nativeDetails: redacted bounded map optional
```

`HealthStatus`:

- `Healthy`: provider-aware probe completed and satisfied policy.
- `Degraded`: provider responded with degraded evidence or only transport-level evidence exists.
- `Unavailable`: provider-aware or transport evidence confirms no usable response.
- `AuthFailed`: an authenticated probe was rejected.
- `Timeout`: configured deadline elapsed without a conclusive result.
- `Maintenance`: policy suppresses health escalation but preserves observations.
- `Unknown`: evidence is missing, incompatible, internally failed, or too stale to classify.

`ObservationQuality` contains evidence level (`ProviderAuthenticated`, `ProviderReadiness`, `TransportOnly`, `Synthetic`, `Unknown`), configuration hash, attempt count, and optional limitations. TCP success is always `Degraded` + `TransportOnly`, never `Healthy`.

## Derived instance state

The server derives current state from observations and policy:

```text
InstanceState
  instanceId
  status
  observedAt
  receivedAt
  staleAfter
  isStale
  sourceAgentId
  observationId
  incidentId optional
```

Stale calculation uses server receipt/expected interval and does not rewrite the source observation. A missing/revoked/offline Agent is separately represented; database state and Agent state are not conflated.

## Canonical event

```text
EventRecord
  eventId: UUID
  eventType: Connected | Disconnected | Timeout | AuthenticationFailed |
             SlowResponse | Degraded | Recovered | AgentOffline |
             AdministrativeCommandCompleted | AdministrativeCommandFailed
  severity: Info | Warning | Error | Critical
  instanceId optional
  agentId optional
  observedAt
  receivedAt
  correlationId
  sourceObservationId optional
  details: bounded redacted map
```

Events are immutable. Corrections or enrichments create linked records rather than overwriting history.

## Normalized error

```text
NormalizedError
  code: stable canonical code
  category: Configuration | Network | Timeout | Authentication | Authorization |
            Provider | Platform | Resource | Compatibility | Internal
  retryability: Never | Backoff | AfterConfigurationChange | Unknown
  safeMessage: localized-at-presentation text key/details
  providerCode: redacted bounded string optional
  diagnosticRef: opaque log/trace reference optional
```

Exceptions and native stderr are not transported wholesale. Provider normalization removes host secrets, usernames when classified, SQL/query content, file-system secrets, and connection strings.

## Capability descriptor

```text
CapabilityDescriptor
  capabilityId: stable versioned identifier
  state: Supported | Unsupported | Unavailable | Unknown
  providerType
  providerVersion
  platform
  prerequisites: list<string>
  reasonCode
  observedAt
```

`Supported` means implemented for the declared combination, not necessarily homologated for public support. Homologation is separate release metadata.

## Administrative command

```text
AdministrativeCommand
  commandId: UUID
  idempotencyKey: scoped unique string
  instanceId
  assignedAgentId
  capabilityId
  typedParameters: provider-defined schema, no free-form shell/SQL
  requestedBy
  requestedAt
  reason
  expiresAt
  authorizationSnapshotRef
  expectedAgentVersion
  expectedProviderVersion
  state: Pending | Available | Acknowledged | Running | Succeeded | Failed |
         Cancelled | Expired | Rejected | UnknownOutcome
```

```text
CommandAttemptResult
  commandId
  attemptId
  startedAt
  completedAt
  adapterResult
  postProbeObservationId optional
  terminalState
  normalizedError optional
  diagnosticRef optional
```

Only the server creates authoritative commands. A terminal retry returns the existing result. `Succeeded` requires the configured post-probe; an inconclusive effect becomes `UnknownOutcome`.

## Agent heartbeat and compatibility

```text
AgentHeartbeat
  agentId
  agentVersion
  protocolMin
  protocolMax
  platform
  providerPackages: list<name,version,hash>
  capabilityClaims: list<CapabilityDescriptor>
  queueDepth
  oldestQueuedAt optional
  localClockAt
  sentAt
```

Heartbeat describes Agent process/connectivity, not database health. Server policy validates versions and claims before command assignment.

## Credential reference

```text
CredentialReference
  referenceId: UUID
  vaultProvider: stable adapter ID
  locator: opaque non-secret locator
  purpose: Monitoring | Administration | OperatingSystemControl | CloudControlPlane
  rotationState
  lastRotatedAt optional
```

The protocol and ordinary persistence never expose resolved secret values.

## Invariants

- All timestamps are UTC instants; display localization is a UI concern.
- All list/batch endpoints have configured upper bounds.
- Config, policy, and command writes use optimistic concurrency.
- IDs and idempotency keys are scoped and unique through constraints in `STATE-03`.
- Unknown enum values are preserved/rejected according to protocol compatibility rules, never silently mapped to healthy/success.
- AIOps/AI consumes sanitized copies of these contracts and cannot invoke a command executor directly.
