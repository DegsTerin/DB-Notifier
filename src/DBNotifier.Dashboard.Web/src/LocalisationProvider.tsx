/**
 * Applies the shared DB-Notifier interface-language contract to the React Dashboard.
 * Persistence contains only a supported BCP 47 locale and fails safely to the pt-BR product baseline.
 */
import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import type { MessageKey, SupportedLocale } from "./generated/localisation";
import { defaultLocale, languagePreferenceStorageKey, parseLocale, persistLocale, readLocale, translate } from "./localisation";

interface LocalisationContextValue {
  locale: SupportedLocale;
  setLocale: (locale: SupportedLocale) => void;
  t: (key: MessageKey, ...values: readonly (string | number)[]) => string;
}

const LocalisationContext = createContext<LocalisationContextValue>({
  locale: defaultLocale,
  setLocale: () => undefined,
  t: (key, ...values) => translate(defaultLocale, key, ...values),
});

/** Reads the pre-render bootstrap result before consulting resilient storage. */
function readInitialLocale(): SupportedLocale {
  const bootstrapped = document.documentElement.dataset.languagePreference;
  if (bootstrapped) return parseLocale(bootstrapped);
  try {
    return readLocale(window.localStorage);
  } catch {
    return defaultLocale;
  }
}

/** Owns interface-language state while leaving route and feature state in their existing components. */
export function LocalisationProvider({ children }: { children: ReactNode }) {
  const [locale, setLocale] = useState<SupportedLocale>(readInitialLocale);
  const value = useMemo<LocalisationContextValue>(() => ({
    locale,
    setLocale,
    t: (key, ...values) => translate(locale, key, ...values),
  }), [locale]);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dataset.languagePreference = locale;
    document.title = translate(locale, "App.Title");
    document.querySelector<HTMLMetaElement>('meta[name="description"]')?.setAttribute(
      "content",
      translate(locale, "App.Description"),
    );
    try {
      persistLocale(window.localStorage, locale);
    } catch {
      // Access to localStorage can itself be denied before the resilient adapter receives it.
    }
  }, [locale]);

  useEffect(() => {
    /** Synchronises supported language changes made in another tab. */
    const handleStorage = (event: StorageEvent) => {
      if (event.key === languagePreferenceStorageKey) setLocale(parseLocale(event.newValue));
    };
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);

  return <LocalisationContext.Provider value={value}>{children}</LocalisationContext.Provider>;
}

/** Returns the active locale, mutator and typed canonical translator. */
export function useLocalisation(): LocalisationContextValue {
  return useContext(LocalisationContext);
}
