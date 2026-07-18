# DB-Notifier provider icon assets

This directory owns third-party visual identity assets used only beside an authoritative provider name. It does not define the provider catalogue, implementation, homologation or public support.

## Coverage and fallback

`manifest.json` maps the exact provider identifiers for which the pinned Skill Icons revision contains a corresponding asset. Every other valid provider identifier remains supported by the presentation contract and receives the DB-Notifier neutral database glyph plus its full text label. A related vendor or platform logo must never be substituted for a missing exact identity.

The source SVGs are vendored under `skill-icons/icons/`. Web and WPF derivatives are generated offline by `scripts/generate-provider-icon-assets.mjs`; product runtime and ordinary builds never download an icon. The verifier applies a static-SVG allowlist, rejects active or external references and fails on unexpected source or derivative files. A pinned SVG may retain an upstream embedded PNG data reference only when its exact source hashes are approved and the canonical base64 value decodes to a bounded, structurally valid PNG.

## Provenance and licence

The source collection is [Skill Icons](https://github.com/tandpfun/skill-icons), pinned to commit `7f7e691e71aec64e8354bf697835e009d1ad80f8` and retrieved on 2026-07-18. Its MIT licence is preserved verbatim in `skill-icons/LICENSE`. The manifest records and the verifier recalculates the upstream Git blob SHA and local SHA-256 for every SVG and the licence.

Names and logos may also be trademarks of their respective owners. Their presence identifies fixture or provider data only; it does not imply affiliation, endorsement, implementation, homologation, health or public support.

## Maintenance

1. Verify an exact upstream identity and its redistribution terms.
2. Pin a reviewed upstream revision and add the source SVG without changing its visual geometry or colours.
3. Extend the presentation-only manifest and both local client registries; do not add engine conditionals to Domain or Application.
4. Run `node scripts/generate-provider-icon-assets.mjs --generate` only with the manifest-pinned rasteriser, then run the same command with `--verify`.
5. Review Light, Dark and High Contrast behaviour. High Contrast must use the neutral system-colour fallback.

A provider icon change never advances a lifecycle state or provider support status by itself.
