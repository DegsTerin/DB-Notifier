/**
 * Applies the persisted Dashboard theme before render-blocking presentation can paint.
 * This standalone bootstrap mirrors the typed runtime contract and fails safely when browser APIs are restricted.
 */
(function applyInitialTheme() {
  const storageKey = "dbnotifier.theme.preference.v1";
  const darkQuery = "(prefers-color-scheme: dark)";
  let preference = "system";

  try {
    const stored = window.localStorage.getItem(storageKey);
    if (stored === "light" || stored === "dark" || stored === "system") {
      preference = stored;
    }
  } catch {
    // Restricted storage is a supported browser state; System remains the safe default.
  }

  let systemUsesDark = false;
  try {
    systemUsesDark = window.matchMedia(darkQuery).matches;
  } catch {
    // Environments without colour-scheme media queries resolve System to Light.
  }

  document.documentElement.dataset.themePreference = preference;
  document.documentElement.dataset.theme = preference === "system"
    ? (systemUsesDark ? "dark" : "light")
    : preference;
})();
