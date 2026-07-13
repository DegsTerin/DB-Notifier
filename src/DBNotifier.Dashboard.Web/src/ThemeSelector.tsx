/**
 * Provides the Dashboard theme runtime and one discreet cycling topbar control.
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

/** Renders the outlined icon for the current System, Light or Dark preference. */
function ThemeIcon({ preference }: Readonly<{ preference: ThemePreference }>) {
  if (preference === "light") {
    return (
      <svg className="preference-icon" viewBox="0 0 24 24" aria-hidden="true">
        <circle cx="12" cy="12" r="3.5" />
        <path d="M12 2.5v3M12 18.5v3M2.5 12h3M18.5 12h3M5.3 5.3l2.1 2.1M16.6 16.6l2.1 2.1M18.7 5.3l-2.1 2.1M7.4 16.6l-2.1 2.1" />
      </svg>
    );
  }
  if (preference === "dark") {
    return (
      <svg className="preference-icon" viewBox="0 0 24 24" aria-hidden="true">
        <path d="M20 15.2A8.3 8.3 0 0 1 8.8 4a8.5 8.5 0 1 0 11.2 11.2Z" />
      </svg>
    );
  }
  return (
    <svg className="preference-icon" viewBox="0 0 24 24" aria-hidden="true">
      <rect x="3.5" y="4.5" width="17" height="12" rx="1.8" />
      <path d="M9 20h6M12 16.5V20" />
    </svg>
  );
}

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
 * Renders the theme cycle control and keeps System responsive to live platform changes.
 * @returns An accessible icon button with an announced current preference.
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

  const currentIndex = options.findIndex((option) => option.value === preference);
  const nextOption = options[(currentIndex + 1) % options.length] ?? options[0];
  const currentLabel = t(options[currentIndex]?.labelKey ?? "Theme.System");
  const nextLabel = t(nextOption.labelKey);
  const accessibleLabel = t("Theme.Toggle", currentLabel, nextLabel);
  return (
    <button
      className="preference-icon-button theme-selector"
      type="button"
      aria-label={accessibleLabel}
      title={accessibleLabel}
      data-preference={preference}
      onClick={() => setPreference(nextOption.value)}
    >
      <ThemeIcon preference={preference} />
      <span className="sr-only" aria-live="polite">{t("Theme.Announcement", currentLabel)}</span>
    </button>
  );
}
