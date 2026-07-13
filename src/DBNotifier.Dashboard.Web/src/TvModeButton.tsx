/**
 * Owns the session-only Dashboard TV presentation mode and optional browser Fullscreen integration.
 * The control never claims an external real-time feed and keeps a visible exit path when Fullscreen is unavailable.
 */
import { useEffect, useRef, useState } from "react";
import { useLocalisation } from "./LocalisationProvider";

/** Renders the current expand or collapse icon without duplicating the button's accessible name. */
function FullscreenIcon({ active }: Readonly<{ active: boolean }>) {
  return active ? (
    <svg className="preference-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M9 4v5H4M15 4v5h5M9 20v-5H4M15 20v-5h5" />
    </svg>
  ) : (
    <svg className="preference-icon" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M9 4H4v5M15 4h5v5M9 20H4v-5M15 20h5v-5" />
    </svg>
  );
}

/**
 * Renders one toggle that synchronises the TV layout with browser Fullscreen when permitted.
 * @param active - Whether the session-only TV presentation is active.
 * @param onActiveChange - Applies the requested presentation state in the owning Dashboard shell.
 * @returns An accessible expand/collapse control plus a polite Fullscreen failure announcement.
 */
export function TvModeButton({ active, onActiveChange }: Readonly<{ active: boolean; onActiveChange: (active: boolean) => void }>) {
  const { t } = useLocalisation();
  const fullscreenWasActive = useRef(false);
  const [fullscreenError, setFullscreenError] = useState("");

  useEffect(() => {
    if (active) document.documentElement.dataset.tvMode = "true";
    else delete document.documentElement.dataset.tvMode;
    return () => { delete document.documentElement.dataset.tvMode; };
  }, [active]);

  useEffect(() => {
    /** Leaves TV presentation when the user exits native Fullscreen through Escape or browser chrome. */
    const handleFullscreenChange = () => {
      if (document.fullscreenElement) {
        fullscreenWasActive.current = true;
      } else if (fullscreenWasActive.current) {
        fullscreenWasActive.current = false;
        onActiveChange(false);
      }
    };
    document.addEventListener("fullscreenchange", handleFullscreenChange);
    return () => document.removeEventListener("fullscreenchange", handleFullscreenChange);
  }, [onActiveChange]);

  /** Toggles TV presentation first, then requests or exits native Fullscreen without making it a requirement. */
  const toggle = async () => {
    setFullscreenError("");
    if (active) {
      onActiveChange(false);
      if (document.fullscreenElement && typeof document.exitFullscreen === "function") {
        try {
          await document.exitFullscreen();
        } catch {
          // The TV layout still exits safely when browser Fullscreen cleanup is denied.
        }
      }
      return;
    }

    onActiveChange(true);
    if (typeof document.documentElement.requestFullscreen !== "function") {
      setFullscreenError(t("TV.FullscreenUnavailable"));
      return;
    }
    try {
      await document.documentElement.requestFullscreen();
    } catch {
      setFullscreenError(t("TV.FullscreenUnavailable"));
    }
  };

  const label = t(active ? "TV.Exit" : "TV.Enter");
  return (
    <>
      <button
        className={`preference-icon-button tv-mode-button ${active ? "active" : ""}`}
        type="button"
        aria-label={label}
        aria-pressed={active}
        title={label}
        data-tv-mode-control={active ? "exit" : "enter"}
        onClick={() => void toggle()}
      >
        <FullscreenIcon active={active} />
      </button>
      <span className="sr-only" role="status" aria-live="polite">{fullscreenError}</span>
    </>
  );
}
