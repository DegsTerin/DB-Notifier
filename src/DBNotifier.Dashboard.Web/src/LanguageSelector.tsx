/**
 * Renders discreet persisted pt-BR and en-GB controls in the Dashboard topbar.
 * Pressed-state semantics preserve keyboard access without relying on flags or arbitrary locale input.
 */
import { useLocalisation } from "./LocalisationProvider";
import type { SupportedLocale } from "./generated/localisation";

/** Renders the supported locale buttons without accepting arbitrary culture input. */
export function LanguageSelector() {
  const { locale, setLocale, t } = useLocalisation();
  return (
    <div className="preference-selector language-selector" role="group" aria-label={t("Language.Label")}>
      {(["pt-BR", "en-GB"] as const satisfies readonly SupportedLocale[]).map((option) => (
        <button
          key={option}
          type="button"
          aria-pressed={locale === option}
          title={option === "pt-BR" ? t("Language.PtBr") : t("Language.EnGb")}
          onClick={() => setLocale(option)}
        >
          {option}
        </button>
      ))}
      <span className="sr-only" aria-live="polite">{t("Language.Announcement", locale === "pt-BR" ? t("Language.PtBr") : t("Language.EnGb"))}</span>
    </div>
  );
}
