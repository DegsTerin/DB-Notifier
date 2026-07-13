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

const options: ReadonlyArray<{ value: ThemePreference; label: string }> = [
  { value: "system", label: "Sistema" },
  { value: "light", label: "Claro" },
  { value: "dark", label: "Escuro" },
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

  const currentLabel = options.find((option) => option.value === preference)?.label ?? "Sistema";
  return (
    <fieldset className="theme-selector">
      <legend>Tema</legend>
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
            <span>{option.label}</span>
          </label>
        ))}
      </div>
      <span className="sr-only" aria-live="polite">Preferência de tema: {currentLabel}</span>
    </fieldset>
  );
}
