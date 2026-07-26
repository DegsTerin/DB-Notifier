# STATE-06 — R-SEQ Durable Rejection Ledger Report

## Status

The durable R-SEQ rejection-ledger implementation and its focused disposable PostgreSQL migration laboratory are
factually complete. The laboratory passed `1/1` on 2026-07-26. The Release build, complete .NET suites, focused
pipeline E2E, coverage, formatting, code-documentation, Markdown-link and secret-scan gates also passed after the
mechanical migration normalisation.

This result does not change `STATE-06 INTEGRATION`, does not activate the product and does not apply a migration to
an existing or operational PostgreSQL database.

## Authority and scope

The authorised lot is limited to the directly affected R-SEQ persistence, migration, regression and proprietary
technical-documentation boundaries. It does not authorise:

- an operational migration or connection to a monitored database;
- normal observation synchronisation runtime;
- lifecycle progression, a Human Gate or any `ActivationState` change;
- a new dependency, push, pull request, deployment or external publication.

The earlier
[R-SEQ remediation report](STATE-06-R-SEQ-Rejected-Observation-Sequence-Remediation-Report.md)
remains historical evidence of the cursor-only correction. It is not rewritten by this increment.

## Residual diagnosis

The original R-SEQ correction allowed a terminal central rejection to consume its contiguous sequence without
creating health evidence. That resolved the sequencing deadlock, but a cursor alone could not retain the rejected
message identity or original reason. A replay at an already covered historical slot therefore could not distinguish
an accepted sample, a consumed rejection whose detail had never existed, or missing durable evidence.

A table alone would not resolve that ambiguity because pre-migration cursor positions cannot be reconstructed
safely. The implemented model therefore combines:

1. a per-Agent inclusive cutover recording the first sequence covered by the durable rejection ledger; and
2. one immutable protocol-evidence row for every rejection consumed at or after that cutover.

## Implemented model

`AgentObservationCursorRow` now carries `RejectionLedgerStartSequence`.

- Existing cursors receive `HighestContiguousSequence + 1`.
- New cursors start at sequence one.
- The cutover is not inferred from retained samples and no historical rejection is synthesised.

`RejectedObservationSequenceRow` maps to `rejected_observation_sequences` with:

| Field | Meaning |
|---|---|
| `agent_id` | owning authenticated Agent stream |
| `sequence` | positive rejected stream position |
| `message_id` | Agent message identity retained for replay classification |
| `error_code` | stable, bounded and sanitised terminal reason |
| `consumed_at` | authoritative Server instant when the slot was durably consumed |

The primary key is `(agent_id, sequence)`. A non-unique `(agent_id, message_id)` index supports replay and
idempotency-conflict classification. The Agent foreign key points to the owning observation cursor with restricted
deletion. `message_id` is deliberately not unique in this table: reuse of an earlier identifier in a later sequence
is itself a terminal idempotency conflict, and that later slot still needs durable resolution.

The ledger stores protocol evidence only. It does not store the rejected payload, secrets or health evidence.

## Transactional behaviour and invariants

The existing per-Agent process fence, PostgreSQL identity lock, serialisable transaction and locked observation
cursor remain the ordering boundary.

| Condition | Durable result |
|---|---|
| rejection is exactly `cursor + 1` | insert ledger row and advance cursor in the same transaction |
| accepted successors already exist | reconcile only those accepted samples after the rejected gap is consumed |
| rejection is above a lower gap | return retryable; create no pending ledger row |
| exact rejected-slot/message replay | return the stored error code without changing `consumed_at` |
| another message uses the rejected slot | return `observation.sequence_conflict` |
| a message ID is reused in another slot | consume that slot as `observation.idempotency_conflict` |
| pre-cutover covered slot has no retained detail | return conservative `observation.sequence_conflict` |
| post-cutover covered slot has neither accepted nor rejected evidence | return retryable `ingestion.rejection_ledger_inconsistent` |
| accepted and rejected evidence coexist in one slot | return retryable `ingestion.rejection_ledger_inconsistent` |
| cursor sequence space is exhausted | return retryable `ingestion.sequence_exhausted` |

A rejected row never creates a `health_sample`, `instance_observation_state`, canonical event, Server outbox message
or notification delivery attributable to the rejected payload. A valid successor accepted before the gap closed may
still be projected afterwards using only its own provenance.

## Migration and rollback

`AddRejectedObservationSequenceLedger` is the ninth Server PostgreSQL migration.

Its `Up` path:

1. adds the cutover column without exposing an intermediate compatible runtime;
2. refuses any legacy cursor already at `9223372036854775807` with
   `ingestion.rejection_ledger_cutover_overflow`;
3. backfills every existing cursor to the first sequence after its current high-water mark;
4. creates the ledger, constraints, restricted cursor relationship and replay index; and
5. leaves the ledger empty rather than fabricating historical identity or reason.

Its `Down` path refuses to discard any retained ledger row with
`ingestion.rejection_ledger_downgrade_blocked`. An empty fixture ledger can be rolled back, but a store containing
rejection evidence requires a forward-fix. Any future operational application would also require a stopped,
version-compatible runtime, backup and separately authorised migration procedure.

## Disposable PostgreSQL evidence

The focused test
`RSeqPostgreSqlMigrationTests.DisposablePostgreSqlProvesRejectedObservationLedgerMigration`
ran through `scripts/run-rseq-postgresql-ledger-lab.ps1` against a locally available `postgres:16-alpine` image
pinned to:

```text
sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb
```

Observed result: `1/1` passed.

The matrix proved:

- upgrade from the preceding eighth migration;
- backfill from legacy cursor `7` to ledger cutover `8`;
- durable consumption and replay of sequence `8` with the original error code and consumption instant;
- refusal of `Down` while the fixture ledger contained evidence;
- successful rollback only after explicit removal of the synthetic fixture row;
- atomic refusal of a `bigint`-maximum cutover without leaving the new column behind; and
- successful reapplication with the expected ninth migration recorded.

The container was bound only to a dynamic IPv4 loopback port, used a temporary random credential file and tmpfs
data directory, ran with bounded CPU, memory and PID limits, and was removed through an exact ownership label. The
runner reported zero owned Docker residue. No image was pulled and no operational or monitored PostgreSQL store was
accessed.

## Validation status

| Gate | Result |
|---|---|
| Focused disposable PostgreSQL migration matrix | `1/1` passed |
| Release solution build | passed with `0` warnings and `0` errors |
| Unit tests | `507/507` passed |
| Architecture tests | `96/96` passed |
| Integration tests | `125/125` passed in the complete repetition |
| WPF tests | `10/10` passed |
| Focused authoritative observation-pipeline E2E | `1/1` passed |
| Coverage | 83.33% lines and 56.18% branches; all 10 mandatory components present |
| Formatting after mechanical migration normalisation | passed |
| Code documentation | passed for 406 comment-capable source files |
| Markdown links | passed for 772 local links in 202 files |
| Secret scan | passed for the non-ignored worktree and available Git history |

The mandatory coverage floors remain 70% lines and 45% branches, with 80% line coverage as a risk-based directional
target. The observed result preserves both floors and exceeds the directional line target without lowering any
component floor.

The first integration execution contained one unrelated temporal O5/R5 diagnostic failure. That diagnostic passed
in isolation, and the complete integration repetition then passed `125/125`; no R-SEQ production or test code was
changed to mask it.

The final shutdown stopped eight verified SDK-local MSBuild/Roslyn helpers and ended with zero DB-Notifier process,
owned listener or R-SEQ/R-EGRESS/R-NET laboratory container.

## Retention and recovery

`rejected_observation_sequences` is excluded from ordinary time-based retention and remains coupled to the Agent
observation cursor. The current Server maintenance implementation also preserves all raw accepted observations until
aggregate-before-delete exists.

Before future deletion of an accepted sample at or after the ledger cutover, the project must introduce a durable
accepted-slot tombstone or equivalent unified resolution ledger. Otherwise a legitimately accepted position would
be indistinguishable from post-cutover corruption.

## Residual limits

- The ledger preserves rejected `message_id`, reason and consumption time, but not a byte-exact digest of the
  rejected payload.
- Positions before the per-Agent cutover retain their historical uncertainty; the migration does not reconstruct
  them.
- The physical evidence covers one disposable loopback PostgreSQL container. It does not prove operational upgrade,
  multi-Server contention, long-running capacity, backup/restore or production rollback.
- The disposable connection used `SSL Mode=Disable`; the laboratory is migration evidence and does not prove
  R-NET, TLS or PKI behaviour.
- Durable rejected rows have no ordinary deletion path. Capacity monitoring and any future compaction require
  separate design and authority.
- Observation synchronisation remains disabled in normal composition.

## Factual outcome

The ninth Server migration and the durable rejected-slot model close the previously recorded lack of central
`MessageId` and rejection-reason evidence for newly consumed positions. The historical boundary is explicit, replay
fails closed when post-cutover evidence is inconsistent, and rollback cannot silently erase retained rejection
evidence.

`STATE-06 INTEGRATION` and `ActivationState=None` remain unchanged. No Human Gate, lifecycle transition,
homologation, operational migration, runtime activation, push, pull request or deployment is inferred.
