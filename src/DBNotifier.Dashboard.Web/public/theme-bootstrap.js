/**
 * Applies the persisted Dashboard theme before render-blocking presentation can paint.
 * This standalone bootstrap mirrors the typed runtime contract and fails safely when browser APIs are restricted.
 */
(function applyInitialTheme() {
  const storageKey = "dbnotifier.theme.preference.v1";
  const languageStorageKey = "dbnotifier.language.preference.v1";
  let preference = "light";

  try {
    const stored = window.localStorage.getItem(storageKey);
    if (stored === "light" || stored === "dark") {
      preference = stored;
    }
  } catch {
    // Restricted storage is a supported browser state; Light remains the safe default.
  }

  document.documentElement.dataset.themePreference = preference;
  document.documentElement.dataset.theme = preference;

  let language = "pt-BR";
  try {
    const storedLanguage = window.localStorage.getItem(languageStorageKey);
    if (storedLanguage === "pt-BR" || storedLanguage === "en-GB") language = storedLanguage;
  } catch {
    // Restricted storage preserves the Brazilian Portuguese product baseline.
  }
  document.documentElement.lang = language;
  document.documentElement.dataset.languagePreference = language;
})();
