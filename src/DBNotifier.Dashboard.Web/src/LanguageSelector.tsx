/**
 * Renders the persisted pt-BR and en-GB interface-language selector.
 * Native selection semantics preserve keyboard access and announce the complete visible language names.
 */
import { useLocalisation } from "./LocalisationProvider";
import type { SupportedLocale } from "./generated/localisation";

/** Renders the supported locale selector without accepting arbitrary culture input. */
export function LanguageSelector() {
  const { locale, setLocale, t } = useLocalisation();
  return (
    <label className="language-selector">
      <span>{t("Language.Label")}</span>
      <select value={locale} onChange={(event) => setLocale(event.target.value as SupportedLocale)}>
        <option value="pt-BR">{t("Language.PtBr")}</option>
        <option value="en-GB">{t("Language.EnGb")}</option>
      </select>
      <span className="sr-only" aria-live="polite">{t("Language.Announcement", locale === "pt-BR" ? t("Language.PtBr") : t("Language.EnGb"))}</span>
    </label>
  );
}
