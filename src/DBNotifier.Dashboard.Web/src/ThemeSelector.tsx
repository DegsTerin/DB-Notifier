/**
 * Provides the Dashboard theme runtime and visible System, Light and Dark preference selector.
 * The component owns browser observation and persistence but does not alter routes, feature state or external systems.
 */
import { useEffect, useState } from "react";
import {
  applyTheme,
  darkColourSchemeMediaQuery,
  parseThemePreference,
  persistThemePreference,
  readThemePreference,
  themePreferenceStorageKey,
  type ThemePreference,
} from "./theme";
import { useLocalisation } from "./LocalisationProvider";
import type { MessageKey } from "./generated/localisation";

const options: ReadonlyArray<{ value: ThemePreference; labelKey: MessageKey }> = [
  { value: "system", labelKey: "Theme.System" },
  { value: "light", labelKey: "Theme.Light" },
  { value: "dark", labelKey: "Theme.Dark" },
];

/**
 * Reads the bootstrap result first so React preserves the preference applied before the first paint.
 * @returns The validated initial preference from the root attribute or resilient storage fallback.
 */
function readInitialPreference(): ThemePreference {
  const bootstrapped = parseThemePreference(document.documentElement.dataset.themePreference);
  if (document.documentElement.dataset.themePreference) return bootstrapped;
  try {
    return readThemePreference(window.localStorage);
  } catch {
    return "system";
  }
}

/**
 * Resolves the current platform colour scheme without assuming matchMedia is available.
 * @returns True only when a supported media query explicitly reports Dark.
 */
function readSystemUsesDark(): boolean {
  try {
    return window.matchMedia(darkColourSchemeMediaQuery).matches;
  } catch {
    return false;
  }
}

/**
 * Renders the theme preference control and keeps System responsive to live platform changes.
 * @returns A native radio group with visible labels and an announced current preference.
 */
export function ThemeSelector() {
  const { t } = useLocalisation();
  const [preference, setPreference] = useState<ThemePreference>(readInitialPreference);
  const [systemUsesDark, setSystemUsesDark] = useState(readSystemUsesDark);

  useEffect(() => {
    try {
      const media = window.matchMedia(darkColourSchemeMediaQuery);
      /** Updates the effective theme only when System is selected through normal React resolution. */
      const handleSystemThemeChange = (event: MediaQueryListEvent) => setSystemUsesDark(event.matches);
      media.addEventListener("change", handleSystemThemeChange);
      setSystemUsesDark(media.matches);
      return () => media.removeEventListener("change", handleSystemThemeChange);
    } catch {
      setSystemUsesDark(false);
      return undefined;
    }
  }, []);

  useEffect(() => {
    applyTheme(document.documentElement, preference, systemUsesDark);
    try {
      persistThemePreference(window.localStorage, preference);
    } catch {
      // Storage access can itself be denied before the resilient adapter receives a value.
    }
  }, [preference, systemUsesDark]);

  useEffect(() => {
    /** Synchronises a valid preference selected in another tab without accepting arbitrary storage values. */
    const handleStorage = (event: StorageEvent) => {
      if (event.key === themePreferenceStorageKey) setPreference(parseThemePreference(event.newValue));
    };
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);

  const currentLabel = t(options.find((option) => option.value === preference)?.labelKey ?? "Theme.System");
  return (
    <fieldset className="theme-selector">
      <legend>{t("Theme.Label")}</legend>
      <div className="theme-options">
        {options.map((option) => (
          <label key={option.value}>
            <input
              type="radio"
              name="theme-preference"
              value={option.value}
              checked={preference === option.value}
              onChange={() => setPreference(option.value)}
            />
            <span>{t(option.labelKey)}</span>
          </label>
        ))}
      </div>
      <span className="sr-only" aria-live="polite">{t("Theme.Announcement", currentLabel)}</span>
    </fieldset>
  );
}
