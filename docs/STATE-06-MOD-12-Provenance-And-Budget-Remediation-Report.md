# STATE-06 MOD-12 Provenance and Budget Remediation Report

## Status and authority

- Date: 2026-07-17
- Workspace lifecycle position: `STATE-06 INTEGRATION`
- MOD-12 mode status: no mode promoted; `none → OBSERVER` remains pending
- Predecessor evidence: [`STATE-06-MOD-12-Authenticated-Provenance-And-Budget-Report.md`](STATE-06-MOD-12-Authenticated-Provenance-And-Budget-Report.md)
- Authority: after a direct read-only review of commit `50c6897`, Bruno authorised a local and isolated remediation limited to revocation rollback protection, budget enforcement before complete corpus materialisation, explicit NIST P-256 validation, distinct trust anchors, corresponding tests and factual documentation
- Explicit exclusions: runtime integration, operational providers, external persistence, LLM, executor, services, workers, APIs, external actions and promotion to `OBSERVER`

This report records the corrective increment only. It neither rewrites the historical evidence for `50c6897` nor authorises an operational Observer.

## Review findings addressed

The direct source review found no Critical or High issue. It identified three Medium gaps and one Low gap in the earlier implementation:

1. a correctly signed older revocation snapshot could satisfy freshness because no trusted revision checkpoint was compared;
2. offline cases copied and sorted telemetry before aggregate budget admission;
3. P-256 was inferred from a 256-bit key size rather than proved from the encoded named-curve identifier;
4. grant and revocation trust roles were separate in configuration but could reuse a key identifier or the same public key.

All four findings are remediated locally in this increment.

## Authenticated revocation checkpoint

`ObserverPolicyRevocationSnapshot` now carries a positive, strictly increasing `SnapshotSequence` inside the revised `observer-policy-revocation.v2` canonical signed payload. `ObserverPolicyTrustConfiguration` independently names the exact trusted revocation series and exact current sequence selected by the authorised application boundary.

Verification occurs only after identity and signature validation. A signed snapshot below the configured sequence fails with `aiops.observer.adapter.provenance_revocation_rollback`; a different series or a sequence ahead of the configured checkpoint fails with `aiops.observer.adapter.provenance_revocation_checkpoint_mismatch`. The exact match then remains subject to the existing UTC freshness and grant/key revocation checks.

This is deterministic rollback protection against the application-configured checkpoint. MOD-12 does not issue, distribute, persist or advance that checkpoint. Any future runtime design must update signed evidence and trusted configuration atomically and provide durable checkpoint storage under a separately authorised boundary.

## Exact NIST P-256 and purpose separation

Trust-anchor construction now parses the DER `SubjectPublicKeyInfo` under strict DER rules and requires:

- the `id-ecPublicKey` algorithm identifier `1.2.840.10045.2.1`;
- the named-curve identifier `1.2.840.10045.3.1.7` for NIST P-256;
- one uncompressed 65-byte EC point with no trailing ASN.1 content;
- successful platform import, complete byte consumption, a 256-bit key and the same exported curve identifier.

Signature verification repeats the imported curve check before ECDSA/SHA-256 verification. Trust configuration also rejects reuse of either the signing-key identifier or canonical public-key material across grant and revocation roles.

No private key, certificate, secret, external trust store or new dependency was introduced. Test signers remain ephemeral and are disposed in memory.

## Admission before telemetry materialisation

Offline case construction now records only the bounded declared count and caller-owned telemetry source. It does not enumerate, copy or sort telemetry. Dataset aggregate sample and worst-case work totals therefore use count metadata only.

The runner applies case, aggregate-sample and work-unit admission before any telemetry source is enumerated. A rejected workload returns zero consumed work and leaves the source unread. After admission, the runner validates and copies only the current bounded case, sorts that single copy deterministically, processes it and then proceeds to the next case. Elapsed-time checks surround the materialisation step as well as every charged unit.

If a caller-owned source changes its count or yields invalid, repeated or segment-mismatched telemetry, the case fails closed with a stable source rejection. The source collection itself remains caller-owned memory; this increment prevents an additional MOD-12-owned full-corpus buffer but does not claim runtime streaming, concurrency or memory homologation.

## Regression coverage

Focused regressions cover:

- an older correctly signed snapshot below the trusted checkpoint;
- a correctly signed snapshot ahead of or outside the configured checkpoint;
- exact NIST P-256 curve-identifier rejection while retaining a 256-bit point shape;
- reused signing-key identity and reused public-key material across trust roles;
- aggregate sample-budget rejection with zero telemetry enumerations and zero consumed work;
- the existing inactive-grant boundary;
- deferred invalid provider/version segment rejection.

The governed nine-case, three-segment corpus and its original detection/adversarial metrics remain unchanged.

## Verification

Environment: Windows, repository SDK .NET `10.0.301`, Release configuration, 2026-07-17.

| Check | Observed result |
|---|---|
| Focused AIOps tests | `71/71` passed |
| Full unit/model/provider/presentation suite | `267/267` passed |
| Architecture suite | `15/15` passed |
| Full solution build | passed with zero warnings and zero errors |
| Coverage gate | passed: `80.41%` lines and `61.17%` branches; floors `70%`/`45%` |
| .NET format/analyser gate | passed with no required changes |
| Code-documentation gate | passed for `208` comment-capable source files, followed by human en-GB/API review |
| Markdown-link gate | passed for `236` local links in `66` Markdown files |
| Secret scan | passed for the current non-ignored worktree and available Git history |
| Git diff gates | worktree and staged diff checks passed |

Online NuGet/npm vulnerability audits were not repeated because no dependency changed and external actions remain excluded. Runtime smoke, Dashboard, WPF, provider and external integration checks are not applicable to this isolated Application remediation and were not executed.

## Scope and architecture confirmation

- No project reference, package or dependency changed.
- No DI registration, runtime service, provider binding, network call, file operation, database or persistence path was introduced.
- No collection, operational telemetry, API, worker, LLM, recommendation, plan, command or executor surface was added.
- Collection, persistence and Observer activation remain immutable `false` policy properties.
- The lifecycle remains `STATE-06 INTEGRATION`; no MOD-12 mode is active.

## Gate classification and remaining conditions

- Local remediation Quality Gate: `APPROVED` for this isolated corrective scope.
- `none → OBSERVER`: `PENDING` and outside this authority.
- Later MOD-12 modes: `NOT AUTHORISED`.
- Human acceptance of this corrective increment: `ACCEPTED` on 2026-07-17, without lifecycle or mode promotion.

Before any future runtime proposal, the architecture must still define authenticated issuance and distribution, atomic checkpoint advancement, durable anti-rollback state, key rotation/revocation, bounded caller-source ingestion, runtime memory/concurrency evidence and dedicated architecture, security, Quality and Human Gates.

## Human acceptance of the remediation

After receiving the commit report, Bruno responded exactly:

`Revisão humana da remediação MOD-12 em STATE-06, commit 6a5f00f: ACEITA. Revisei o relatório e os resultados automáticos. Aceito as limitações registradas. Não autorizo promoção para OBSERVER nem novo incremento.`

This decision accepts only the corrective increment recorded in commit `6a5f00f` and its stated limitations. It does not activate MOD-12, approve `none → OBSERVER`, authorise another increment, alter the lifecycle state or waive any future architecture, security, Quality or Human Gate.
