# JOSE-0 IANA snapshot evidence

## Scope and authority

This directory retains public, non-secret standards evidence captured for the
documentary `JOSE-0` lot. It is not executable configuration, runtime trust,
an algorithm allowlist or evidence that a JOSE profile is implemented.

The encoded archive contains the exact HTTPS response bodies for the nine
IANA JOSE CSV registries and the IANA JWT Claims CSV used by
[the coverage matrix](../../JOSE-IANA-Registry-Coverage.md). It deliberately
contains no private key, seed, symmetric secret, token or RFC private test
vector.

## Archive identity

- Encoded file:
  [`iana-jose-jwt-snapshots-2026-07-28.zip.b64`](iana-jose-jwt-snapshots-2026-07-28.zip.b64)
- Base64 whitespace: insignificant
- Decoded ZIP bytes: `10,146`
- Decoded ZIP SHA-256:
  `f3fcf5a876beb5e06b5d00e7a4affcf74f036d3826539e4f8c5a89e2177d22ad`
- ZIP entries: `10`
- Entry timestamp convention: ZIP/DOS calendar date equal to the registry
  `Last Updated` date, with wall-clock field `00:00:00`; ZIP/DOS carries no
  UTC offset, so the value must not be suffixed with `Z` or treated as an
  HTTP timestamp

| ZIP entry | Exact body bytes | Exact body SHA-256 |
|---|---:|---|
| `jose/web-signature-encryption-header-parameters.csv` | 3,394 | `e66121e940ab248a4a60c58534c15f7d92f86e4974b5812af4928f053d99e617` |
| `jose/web-signature-encryption-algorithms.csv` | 6,027 | `5a09ebf769cc2b3ccbec86a6d5a6c2be441c371cc0c136626463727884c0b7fc` |
| `jose/web-encryption-compression-algorithms.csv` | 121 | `2cdae9003156eedf57c47aa3bc73b0ad94fd0a441185e2f8136abd9f01f7bd90` |
| `jose/web-key-types.csv` | 401 | `6511cf891ad75c1845a26c495ca9beee0869033490c1b3f72e96127a45e12814` |
| `jose/web-key-elliptic-curve.csv` | 678 | `8505343ad98e91e449ea3d7ec4728fbb9c3e28a8d433f05f72c189e2659f7251` |
| `jose/web-key-parameters.csv` | 2,589 | `2ce0708246fe33ebc5515faa3bf7017f97c52c6f82232769a3738b83e685a73a` |
| `jose/web-key-use.csv` | 172 | `5be9a2ab909a39a13b3672af90d9031ce949885aaec5a965ef9198158b44522d` |
| `jose/web-key-operations.csv` | 656 | `95dd71016fbb4669758940b194f691bf4cbcf52c25a2c474a4961377a0a0e80a` |
| `jose/web-key-set-parameters.csv` | 124 | `5b6ca5109e444f380d47cb2bc28f67bc0c6100049e116d05580dc256d029d764` |
| `jwt/claims.csv` | 18,690 | `a87f3c08c02b671823e937c7880d29c0cb8c7a5fd518063ab1dec594660f799b` |

## Read-only reconstruction check

The archive can be verified in memory without retaining decoded files:

```powershell
$encoded = Get-Content `
  -LiteralPath '.\docs\architecture\evidence\jose-0\iana-jose-jwt-snapshots-2026-07-28.zip.b64' `
  -Raw
$archiveBytes = [Convert]::FromBase64String($encoded -replace '\s', '')
$archiveHash = [Convert]::ToHexString(
  [Security.Cryptography.SHA256]::HashData($archiveBytes)
).ToLowerInvariant()
$archiveHash
```

Expected result:

```text
f3fcf5a876beb5e06b5d00e7a4affcf74f036d3826539e4f8c5a89e2177d22ad
```

Decoding or extraction is a verification action only. It never authorises
the contents as a runtime profile or permits automatic registry refresh.
