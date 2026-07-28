# PF-OBS-1-D6 — Post-Format Non-Intrusive Rebaseline Protocol

## Authority and purpose

- Version: `pfobs1-d6-post-format-non-intrusive-rebaseline-1.0.0`.
- SHA-256:
  `5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845`.
- V3 dependency:
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Lifecycle: `STATE-06 INTEGRATION`.
- MOD-12 activation: `ActivationState=None`.
- Scope: local, synthetic and marker-gated rebaseline after the development
  machine was reformatted.

D6 determines whether the historical `Cancellation/Cold` working-set excess
can be reproduced in the current environment without adding instrumentation
inside the measured process. It is diagnostic evidence only. It is not an
HM-01–HM-03 campaign, does not change V3 and cannot authorise Observer
activation.

## Canonical statement

```text
pfobs1-d6-post-format-non-intrusive-rebaseline|1.0.0|v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|runs=2|arm=unobserved|precondition=v3-exact|prefix=first-byte-cold-5-30,first-byte-warm-5-30,idle-cold-5-30,idle-warm-5-30,cancellation-cold-5-13|summaries=original-four|intraprocess-extra-captures=0|working-set-limit=786432|samples=all|forced-gc=v3-two-existing-only|activation=none|conditional=external-after-two-reproductions
```

## Frozen unobserved arm

Each of two consecutive runs executes in a fresh process:

1. the unchanged V3 precondition;
2. `FirstByte/Cold`, five warm-ups and thirty measured samples;
3. the original `FirstByte/Cold` summary;
4. `FirstByte/Warm`, five warm-ups and thirty measured samples;
5. the original `FirstByte/Warm` summary;
6. `Idle/Cold`, five warm-ups and thirty measured samples;
7. the original `Idle/Cold` summary;
8. `Idle/Warm`, five warm-ups and thirty measured samples;
9. the original `Idle/Warm` summary;
10. `Cancellation/Cold`, five warm-ups and measured repetitions 1–13.

The resulting prefix contains exactly `158` samples and four summaries. The
runner stores the existing V3 sample and summary records only after each
operation completes. It performs no additional in-process working-set,
virtual-memory, thread, handle or process-state capture.

The original measurement source remains unchanged. Its existing before,
checkpoint and after observations are part of V3 and are not additional D6
instrumentation.

## Environment declaration

Each report records only sanitised facts needed to interpret the rebaseline:

- operating-system version and architecture;
- process architecture;
- .NET runtime and repository-selected SDK versions;
- logical processor count;
- installed physical-memory quantity;
- monotonic-clock frequency.

The report must not retain a machine, host, user or device name, process
identifier, local path, payload, credential or topology.

## Reproduction and conditional gate

The unchanged inclusive working-set limit is `786,432 bytes`.

- One run reproduces the excess only when a complete
  `Cancellation/Cold` sample exceeds that limit.
- The unobserved arm reproduces the historical condition only when both
  fresh-process runs reproduce it.
- External observation is permitted only after both unobserved runs
  reproduce.
- Repetition of the historical D5 is permitted only as a comparison after the
  same two-run reproduction gate.

If either unobserved run does not reproduce, D6 stops. No external arm, D5
comparison or replacement run follows.

## Stop rules and prohibitions

D6 stops without interpretation when:

- the protocol or V3 digest differs;
- a sample or one of the four original summaries is missing;
- the exact prefix order changes;
- an additional in-process capture appears;
- a dependency, restore, download or external tool is required;
- cleanup cannot prove zero owned process or temporary residue; or
- the current environment cannot be declared without retaining prohibited
  identity.

D6 must not use PostgreSQL, a provider, operational data, normal composition,
`EmptyWorkingSet`, forced collection beyond the two existing V3 collections,
priority or affinity changes, threshold changes, favourable sample selection,
hidden samples, Observer activation, lifecycle transition, deployment, push
or pull request.
