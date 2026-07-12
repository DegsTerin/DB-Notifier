/**
 * Defines the Dashboard side of the shared Light, Dark and System preference contract.
 * Persistence and DOM application are intentionally deferred to the next Design System increment.
 */
export type ThemePreference = "system" | "light" | "dark";
export type EffectiveTheme = "light" | "dark";

export const themePreferenceStorageKey = "dbnotifier.theme.preference.v1";
export const themePreferenceSchemaVersion = "dbnotifier.ui-preferences.v1";

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
