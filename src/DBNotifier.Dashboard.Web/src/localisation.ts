/**
 * Provides framework-neutral access to the generated DB-Notifier localisation catalogue.
 * It validates persisted preferences and formats only keys declared by the canonical source.
 */
import { localisationMessages, type MessageKey, type SupportedLocale } from "./generated/localisation.ts";

export const languagePreferenceStorageKey = "dbnotifier.language.preference.v1";
export const defaultLocale: SupportedLocale = "pt-BR";
export type LanguageStorage = Pick<Storage, "getItem" | "setItem">;

/** Parses untrusted preference data without accepting arbitrary browser locales. */
export function parseLocale(value: string | null | undefined): SupportedLocale {
  return value === "en-GB" || value === "pt-BR" ? value : defaultLocale;
}

/** Reads a locale from potentially restricted storage and falls back to pt-BR. */
export function readLocale(storage: LanguageStorage | null | undefined): SupportedLocale {
  try {
    return parseLocale(storage?.getItem(languagePreferenceStorageKey));
  } catch {
    return defaultLocale;
  }
}

/** Persists a validated locale without allowing browser policy failures to interrupt rendering. */
export function persistLocale(storage: LanguageStorage | null | undefined, locale: SupportedLocale): boolean {
  try {
    if (!storage) return false;
    storage.setItem(languagePreferenceStorageKey, locale);
    return true;
  } catch {
    return false;
  }
}

/** Formats one canonical message using invariant positional placeholders. */
export function translate(locale: SupportedLocale, key: MessageKey, ...values: readonly (string | number)[]): string {
  return localisationMessages[locale][key].replace(/\{(\d+)\}/g, (placeholder, index: string) =>
    values[Number(index)] === undefined ? placeholder : String(values[Number(index)]));
}
