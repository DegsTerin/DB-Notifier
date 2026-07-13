/**
 * Renders one discreet persisted language control in the Dashboard topbar.
 * The generic language icon avoids flags while its accessible name exposes the current and next supported locale.
 */
import { useLocalisation } from "./LocalisationProvider";
import type { SupportedLocale } from "./generated/localisation";

/** Renders the code-native language icon shared by the compact preference button. */
function LanguageIcon() {
  return (
    <svg className="preference-icon" viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="12" cy="12" r="8.5" />
      <path d="M3.8 12h16.4M12 3.5c2.2 2.3 3.4 5.1 3.4 8.5S14.2 18.2 12 20.5M12 3.5C9.8 5.8 8.6 8.6 8.6 12s1.2 6.2 3.4 8.5" />
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
