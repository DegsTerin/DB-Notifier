/**
 * Defines and applies the explicit Dashboard Light and Dark preference contract.
 * Storage failures and retired System values fail safely to Light without consulting platform colour preferences.
 */
export type ThemePreference = "light" | "dark";
export type EffectiveTheme = "light" | "dark";

export type ThemeStorage = Pick<Storage, "getItem" | "setItem">;
export type ThemeRoot = Pick<HTMLElement, "dataset">;

export const themePreferenceStorageKey = "dbnotifier.theme.preference.v1";
export const themePreferenceSchemaVersion = "dbnotifier.ui-preferences.v1";

/**
 * Parses an untrusted persisted value and migrates retired or invalid values to Light.
 * @param value Persisted storage value or null when storage is unavailable.
 * @returns A recognised preference suitable for theme resolution.
 */
export function parseThemePreference(value: string | null | undefined): ThemePreference {
  return value === "dark" ? "dark" : "light";
}

/**
 * Resolves one explicit preference to its matching semantic token theme.
 * @param preference Validated Light or Dark preference.
 * @returns The semantic token theme to apply.
 */
export function resolveEffectiveTheme(preference: ThemePreference): EffectiveTheme { return preference; }

/**
 * Reads a preference from potentially unavailable browser storage.
 * @param storage Browser storage or null when the environment does not expose it.
 * @returns A recognised preference, defaulting to Light after any access or validation failure.
 */
export function readThemePreference(storage: ThemeStorage | null | undefined): ThemePreference {
  try {
    return parseThemePreference(storage?.getItem(themePreferenceStorageKey));
  } catch {
    return "light";
  }
}

/**
 * Persists a validated preference without allowing quota or privacy failures to interrupt the interface.
 * @param storage Browser storage or null when persistence is unavailable.
 * @param preference The explicit user-selected preference.
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
 * @returns The effective Light or Dark theme applied to the root.
 */
export function applyTheme(
  root: ThemeRoot,
  preference: ThemePreference,
): EffectiveTheme {
  const effectiveTheme = resolveEffectiveTheme(preference);
  root.dataset.themePreference = preference;
  root.dataset.theme = effectiveTheme;
  return effectiveTheme;
}
