# DB-Notifier Design Tokens

This directory is the canonical, platform-neutral source for DB-Notifier visual tokens. The normative design and accessibility rules are defined in [`../docs/design/DB-Notifier-Design-System.md`](../docs/design/DB-Notifier-Design-System.md).

## Commands

From `src/DBNotifier.Dashboard.Web`:

```powershell
npm run tokens:generate
npm run tokens:verify
```

`tokens:generate` validates the schema, references, token types and Light/Dark parity before producing deterministic CSS and WPF resource dictionaries. `tokens:verify` fails when committed generated output differs from the canonical source.

## Ownership rules

- Edit only `schema/` and `tokens/` when changing the token contract.
- Never edit generated CSS/XAML manually.
- Keep semantic names identical between Light and Dark.
- Add component tokens instead of consuming palette primitives in feature UI.
- Update the Design System version and contrast/visual evidence with every material token change.

Generated adapters are not yet applied to the current React/WPF views; integration is the next controlled `STATE-05` increment.
