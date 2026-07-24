/** Module purpose: Declares the single compile-time guard accepted by the local Dashboard TV sandbox adapter. */
interface ImportMetaEnv {
  readonly VITE_DB_NOTIFIER_TV_SANDBOX?: string;
  readonly VITE_DB_NOTIFIER_OBSERVER_SANDBOX?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
