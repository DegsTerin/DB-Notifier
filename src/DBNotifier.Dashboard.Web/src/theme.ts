/**
 * Defines and applies the Dashboard Light, Dark and System preference contract.
 * Storage failures fail safely to System, while the effective theme remains a transient platform-derived value.
 */
export type ThemePreference = "system" | "light" | "dark";
export type EffectiveTheme = "light" | "dark";

export type ThemeStorage = Pick<Storage, "getItem" | "setItem">;
export type ThemeRoot = Pick<HTMLElement, "dataset">;

export const themePreferenceStorageKey = "dbnotifier.theme.preference.v1";
export const themePreferenceSchemaVersion = "dbnotifier.ui-preferences.v1";
export const darkColourSchemeMediaQuery = "(prefers-color-scheme: dark)";

/**
 * Parses an untrusted persisted value and fails safely to the System preference.
 * @param value Persisted storage value or null when storage is unavailable.
 * @returns A recognised preference suitable for theme resolution.
 */
export function parseThemePreference(value: string | null | undefined): ThemePreference {
  return value === "light" || value === "dark" || value === "system" ? value : "system";
}

/**
 * Resolves a preference without converting System into a persisted effective theme.
 * @param preference Validated Light, Dark or System preference.
 * @param systemUsesDark Whether the operating system currently requests a dark colour scheme.
 * @returns The semantic token theme to apply.
 */
export function resolveEffectiveTheme(preference: ThemePreference, systemUsesDark: boolean): EffectiveTheme {
  if (preference === "light" || preference === "dark") return preference;
  return systemUsesDark ? "dark" : "light";
}

/**
 * Reads a preference from potentially unavailable browser storage.
 * @param storage Browser storage or null when the environment does not expose it.
 * @returns A recognised preference, defaulting to System after any access or validation failure.
 */
export function readThemePreference(storage: ThemeStorage | null | undefined): ThemePreference {
  try {
    return parseThemePreference(storage?.getItem(themePreferenceStorageKey));
  } catch {
    return "system";
  }
}

/**
 * Persists a validated preference without allowing quota or privacy failures to interrupt the interface.
 * @param storage Browser storage or null when persistence is unavailable.
 * @param preference The user-selected preference, never the resolved effective theme.
 * @returns True when persistence succeeded; otherwise false.
 */
export function persistThemePreference(
  storage: ThemeStorage | null | undefined,
  preference: ThemePreference,
): boolean {
  try {
    if (!storage) return false;
    storage.setItem(themePreferenceStorageKey, preference);
    return true;
  } catch {
    return false;
  }
}

/**
 * Applies preference and effective-theme attributes consumed by the generated token adapter.
 * @param root The Dashboard document root.
 * @param preference The persisted semantic preference.
 * @param systemUsesDark Whether the current platform requests a dark colour scheme.
 * @returns The effective Light or Dark theme applied to the root.
 */
export function applyTheme(
  root: ThemeRoot,
  preference: ThemePreference,
  systemUsesDark: boolean,
): EffectiveTheme {
  const effectiveTheme = resolveEffectiveTheme(preference, systemUsesDark);
  root.dataset.themePreference = preference;
  root.dataset.theme = effectiveTheme;
  return effectiveTheme;
}
