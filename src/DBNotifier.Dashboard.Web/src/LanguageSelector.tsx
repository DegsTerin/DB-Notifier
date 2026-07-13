/**
 * Renders one discreet persisted language control in the Dashboard topbar.
 * The translation icon avoids flags and globe ambiguity while its accessible name exposes the current and next supported locale.
 */
import { useLocalisation } from "./LocalisationProvider";
import type { SupportedLocale } from "./generated/localisation";

/** Renders the code-native translation icon shared by the compact preference button. */
function LanguageIcon() {
  return (
    <svg className="preference-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M2 5h12M7 2h1M5 8l6 6M4 14l6-6 2-3M12 22l5-10 5 10M14 18h6" />
    </svg>
  );
}

/** Renders one keyboard-operable locale-cycle button without accepting arbitrary culture input. */
export function LanguageSelector() {
  const { locale, setLocale, t } = useLocalisation();
  const nextLocale: SupportedLocale = locale === "pt-BR" ? "en-GB" : "pt-BR";
  const currentLabel = locale === "pt-BR" ? t("Language.PtBr") : t("Language.EnGb");
  const nextLabel = nextLocale === "pt-BR" ? t("Language.PtBr") : t("Language.EnGb");
  const accessibleLabel = t("Language.Toggle", currentLabel, nextLabel);

  return (
    <button
      className="preference-icon-button language-selector"
      type="button"
      aria-label={accessibleLabel}
      title={accessibleLabel}
      data-locale={locale}
      onClick={() => setLocale(nextLocale)}
    >
      <LanguageIcon />
      <span className="sr-only" aria-live="polite">{t("Language.Announcement", currentLabel)}</span>
    </button>
  );
}
