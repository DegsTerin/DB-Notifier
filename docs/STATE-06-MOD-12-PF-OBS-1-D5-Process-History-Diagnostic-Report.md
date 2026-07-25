# PF-OBS-1-D5 — Process-History Diagnostic Report

## Decision

PF-OBS-1-D5 is `BLOCKED`.

The exact process prefix preceding the retained post-D3
`Cancellation/Cold` failure completed twice, but neither fresh-process run
reproduced the historical `2,916,352`-byte working-set peak. The two runs of
each control variant also remained below the unchanged inclusive limit of
`786,432 bytes`.

Because reproduction was mandatory before attribution, no resource or
accumulated phase can be identified as the cause. No V3 workload or protocol
correction was applied, no physical campaign was executed and
`ActivationState=None` remains unchanged.

## Frozen method

The
[D5 diagnostic protocol](STATE-06-MOD-12-PF-OBS-1-D5-Process-History-Diagnostic-Protocol.md)
was frozen before execution as
`pfobs1-d5-process-history-diagnostic-1.0.0`, SHA-256
`82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA`.

Each variant executed in two fresh processes:

| Variant | Samples per process | Purpose |
|---|---:|---|
| `ExactPrefix` | `158` | Reproduce the exact V3 history through measured Cancellation/Cold repetition 13 |
| `WithoutFirstByte` | `88` | Test whether accumulated FirstByte state is necessary |
| `WithoutIdle` | `88` | Test whether accumulated Idle state is necessary |
| `CancellationOnly` | `18` | Test whether Cancellation/Cold is sufficient by itself |

All variants executed the same unchanged V3 precondition. The two existing
`GC.Collect` calls were preserved in their original positions and executed
only as mandatory parts of that precondition. No other forced collection
was introduced or executed.

## Results

| Variant | Run | Complete samples | Maximum Cancellation/Cold working-set delta | Limit | Reproduced |
|---|---:|---:|---:|---:|---|
| `ExactPrefix` | `1` | `158/158` | `12,288 bytes` | `786,432 bytes` | no |
| `ExactPrefix` | `2` | `158/158` | `12,288 bytes` | `786,432 bytes` | no |
| `WithoutFirstByte` | `1` | `88/88` | `20,480 bytes` | `786,432 bytes` | no |
| `WithoutFirstByte` | `2` | `88/88` | `12,288 bytes` | `786,432 bytes` | no |
| `WithoutIdle` | `1` | `88/88` | `45,056 bytes` | `786,432 bytes` | no |
| `WithoutIdle` | `2` | `88/88` | `49,152 bytes` | `786,432 bytes` | no |
| `CancellationOnly` | `1` | `18/18` | `139,264 bytes` | `786,432 bytes` | no |
| `CancellationOnly` | `2` | `18/18` | `176,128 bytes` | `786,432 bytes` | no |

All `704` preregistered samples were retained. No sample failed a V3
threshold. The larger deltas in the isolated control are consistent with
first creation of cancellation-related process resources, while the exact
prefix had already exercised those resources during preconditioning and
earlier history. This observation does not explain the prior
`2,916,352`-byte spike and is not a causal attribution.

## Resource observations

Every sample retained before/after counters for:

- working set and private memory;
- managed heap and GC committed memory;
- operating-system and ThreadPool thread counts;
- handles and cumulative managed allocation;
- committed private, mapped, image and unknown virtual-memory regions.

For the two exact-prefix runs, maximum per-sample Cancellation/Cold deltas
were:

| Metric | Run 1 | Run 2 |
|---|---:|---:|
| Working-set peak | `12,288 bytes` | `12,288 bytes` |
| Private memory | `12,288 bytes` | `12,288 bytes` |
| GC committed | `0 bytes` | `0 bytes` |
| OS threads | `0` | `0` |
| ThreadPool threads | `0` | `0` |
| Handles | `0` | `0` |
| Committed private regions | `12,288 bytes` | `12,288 bytes` |

The process precondition itself changed the working set by approximately
`4.56–4.66 MB`, but that state existed before every retained D5 sample and
before all four controlled variants. It does not reproduce a per-sample
Cancellation/Cold excess and cannot be selected as the historical cause.

## Evidence retention

Eight complete reports were retained below
`artifacts/pf-obs-1-d5/3d7d63740d50469e8cc34f8360ed6056/` before temporary
cleanup:

| File | Bytes | SHA-256 |
|---|---:|---|
| `exact-prefix-run-1.json` | `279,305` | `DE00FA8E2763BE2C210E5C2CD783AF82963364BC4D4BD3FAFEEE91A1C0DA8F89` |
| `exact-prefix-run-2.json` | `279,406` | `5759A4BCE10F2A37F9F2515FA1E4342C2F37083C695C2080D4D532A95F3FDF74` |
| `without-first-byte-run-1.json` | `155,676` | `0E1A32B23449F95546D10D89847DDC63037A8785D3D58ABF275383FF5B0802FF` |
| `without-first-byte-run-2.json` | `155,772` | `9292C830AB267F2C93EE2456AB12A6A91C95233DF17DB9202BA17265A1E89523` |
| `without-idle-run-1.json` | `156,704` | `33DDF8D75460FFC71A4E4587E4DF366D96D7D8A59111100B2712B88B5C2FF921` |
| `without-idle-run-2.json` | `156,755` | `1C9AF1F2A59502D25F8C4784451B9328ECE91AE6D2202A29219DCBD6DA3361FF` |
| `cancellation-only-run-1.json` | `32,956` | `CBDB069E30E344B017082100E588A4BBC7713C087DDC50C57DE591EBDC645FB5` |
| `cancellation-only-run-2.json` | `32,962` | `E5DADA25FA91BD8E577F76688787344D3C2DFD278E97AF41318A24FFF9728875` |

The total retained evidence is `1,249,536 bytes`. Every report declares
`ActivationState=None`, the exact D5/V3 digests, its expected and completed
sample counts and a non-authorising disposition.

## Implementation disposition

The D5 entrypoint, process-state reader, matrix and atomic evidence writer
exist only in the integration-test assembly and the existing consolidated
sandbox host behind the exact marker
`pf-obs-1-d5-process-history-diagnostic-test-only`.

The only change to the V3 driver is making its existing precondition
callable from the D5 test boundary. The method body, two existing forced
collections, workloads, scenarios, SLOs and protocol digest are unchanged.
Normal `src/` composition contains zero D5 reference.

## Validation

| Check | Result |
|---|---|
| D5 focal integration tests | passed, `6/6` |
| Complete integration suite | passed, `124/124` |
| D5 architecture isolation | passed, `1/1` |
| Complete architecture suite | passed, `92/92` |
| Release solution build with `--no-restore` | passed, zero warnings/errors |
| D5 matrix | complete, eight fresh processes and `704/704` samples |
| Documentation gate | passed, `382` files |
| Markdown links | passed, `819` local links in `195` files |
| Current-worktree secret scan | passed |
| Formatting gate | passed at warning severity |
| Node.js toolchain | `v24.18.0` |
| Dependencies and lockfiles | unchanged |
| External access, restore or download | none |
| PostgreSQL, physical HM campaign or Observer runtime | not executed |

An initial build command named the absent `DBNotifier.slnx` and failed
before build execution. It was immediately corrected to the existing
`DBNotifier.sln`; the offline Release build then passed. No project state
was changed by the mistyped command.

## Cleanup

- the exact temporary D5 root was removed after all reports were retained;
- no `.tmp` evidence sibling remains;
- no D5 process, DB-Notifier product process or owned listener remains;
- no PostgreSQL, Docker, browser, provider, database or external runtime
  was started.

## Consequence

D5 cannot be approved because its mandatory two-run reproduction and causal
attribution were not achieved. Its correct fail-closed disposition is
`BLOCKED`.

A Human Gate may accept this blocked result without converting it into a
technical approval. Any further diagnostic, methodological change or new
physical campaign requires separate explicit authorisation.
