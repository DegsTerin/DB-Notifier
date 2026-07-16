/**
 * Validates and serialises the Dashboard's local hash routes without accepting inherited object properties.
 * Routing remains browser-local presentation state and never carries credentials, provider configuration or external commands.
 */

export const dashboardViews = [
  "overview",
  "inventory",
  "alerts",
  "performance",
  "history",
  "configuration",
  "providers",
  "settings",
] as const;

export type DashboardView = typeof dashboardViews[number];

interface DashboardRouteTarget {
  readonly location: { readonly hash: string };
  addEventListener(type: "hashchange" | "popstate", listener: () => void): void;
  removeEventListener(type: "hashchange" | "popstate", listener: () => void): void;
}

interface SkipLinkEvent {
  preventDefault(): void;
}

interface MainContentTarget {
  focus(options?: FocusOptions): void;
  scrollIntoView(options?: ScrollIntoViewOptions): void;
}

const dashboardViewAllowlist: ReadonlySet<string> = new Set(dashboardViews);

/**
 * Resolves one untrusted URL hash to a known Dashboard destination.
 * @param hash - Browser hash including or excluding its leading number sign.
 * @returns The exact allow-listed destination, or Overview for empty, inherited and unknown values.
 */
export function parseDashboardView(hash: string | null | undefined): DashboardView {
  const candidate = (hash ?? "").replace(/^#/, "");
  return dashboardViewAllowlist.has(candidate) ? candidate as DashboardView : "overview";
}

/**
 * Creates the canonical browser hash for one validated Dashboard destination.
 * @param view - Allow-listed local Dashboard destination.
 * @returns A hash suitable for browser history state.
 */
export function dashboardViewHash(view: DashboardView): `#${DashboardView}` {
  return `#${view}`;
}

/**
 * Subscribes one route synchroniser to both explicit hash changes and browser history traversal.
 * @param target - Browser-like route event source whose current hash is treated as untrusted input.
 * @param onView - Consumer invoked with an allow-listed destination after each navigation event.
 * @returns A cleanup callback that removes both event listeners.
 */
export function subscribeDashboardRoute(target: DashboardRouteTarget, onView: (view: DashboardView) => void): () => void {
  const synchronise = () => onView(parseDashboardView(target.location.hash));
  target.addEventListener("hashchange", synchronise);
  target.addEventListener("popstate", synchronise);
  return () => {
    target.removeEventListener("hashchange", synchronise);
    target.removeEventListener("popstate", synchronise);
  };
}

/**
 * Moves focus to the main region while preserving the Dashboard route hash and history entry.
 * @param event - Skip-link activation whose default hash replacement must be cancelled.
 * @param target - Main content region, or null while the region is not mounted.
 * @returns Nothing; a missing target is handled safely after cancelling the route-changing default.
 */
export function focusDashboardMain(event: SkipLinkEvent, target: MainContentTarget | null): void {
  event.preventDefault();
  target?.focus({ preventScroll: true });
  target?.scrollIntoView({ block: "start" });
}
