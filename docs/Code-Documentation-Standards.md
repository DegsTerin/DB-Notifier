# Code Documentation Standards

All hand-written DB-Notifier source and comment-capable configuration files use concise British English (`en-GB`) documentation. Module headers describe responsibility and architectural boundaries. Public C# APIs use XML documentation, while functions, classes and non-obvious rules in other languages use the language's native documentation syntax.

Comments explain intent, assumptions, failure behaviour, limitations and side effects rather than restating syntax. A code change must update or remove comments that no longer match the implementation.

## Automated gate

`npm run comments:verify` from `src/DBNotifier.Dashboard.Web` validates the project-wide source inventory. It requires an early module comment in every comment-capable hand-written file and checks the explicit exception inventory below. British English, API completeness and the technical value of documentation remain review requirements because vocabulary-only automation cannot reliably distinguish prose from identifiers or established protocol names.

Existing APIs are documented incrementally when their owning code is changed. New or modified public C# APIs require XML documentation; new or modified functions and classes in other languages require their native documentation form. This avoids filling the historical codebase with generic text merely to satisfy a counter while still making the standard mandatory for active work.

## Format and immutability exceptions

These files cannot safely receive inline comments or must remain generated/immutable. The gate recognises only these exact categories; new files are not silently exempted.

- Strict JSON: `config/appsettings.json`, runtime `appsettings.json` files, `global.json`, package manifests, package lockfiles, TypeScript configuration and debug settings.
- Generated C#: `*.Designer.cs` and `*ModelSnapshot.cs`.
- Applied EF migrations: the six timestamped migration implementation files under the Agent SQLite and Server PostgreSQL migration directories.
- Generated/build artefacts: `.dotnet/`, `bin/`, `obj/`, `dist/`, `node_modules/` and Git internals.
- Exact tool formats without a portable comment syntax: `DBNotifier.sln`, `.gitignore`, `.gitattributes`, binary/image assets and licence text.

Markdown product documentation is governed editorially. Inline code examples and code-focused Markdown explanations follow the same en-GB vocabulary, but prose documents do not require source-style module headers.
