/** Module purpose: Keeps browser brand assets synchronised with the provider-neutral fleet aggregate without external state. */
import type { FleetAggregateState } from "./presentation";

/** Current visual-contract revision used to invalidate browser associations deterministically. */
export const designSystemVersion = "2.6.9";

/** Stable DOM identity shared by the fail-safe HTML node and runtime semantic replacement. */
export const semanticFaviconId = "dbnotifier-favicon";

/** Generated header and favicon paths that represent one provider-neutral aggregate. */
export type SemanticBrandAssets = Readonly<{
  iconPath: string;
  faviconPath: string;
}>;

/**
 * Resolves the generated header and favicon assets for one factual fleet aggregate.
 * @param state - Canonical provider-neutral aggregate selected from current freshness-aware evidence.
 * @returns Versioned asset paths that share the same semantic bell state.
 */
export function semanticBrandAssets(state: FleetAggregateState): SemanticBrandAssets {
  const revision = `${designSystemVersion}-${state}`;
  return {
    iconPath: `/dbnotifier-icon.${state}.svg?v=${revision}`,
    faviconPath: `/dbnotifier-favicon.${state}.ico?v=${revision}`,
  };
}

/**
 * Replaces favicon candidates so Chromium re-evaluates the state-specific icon and reduces reuse of an old page association.
 * @param ownerDocument - Current Dashboard document that owns the head and initial fail-safe favicon.
 * @param href - Versioned semantic favicon path resolved for the same aggregate as the header mark.
 * @param state - Aggregate recorded on the node for deterministic browser and regression inspection.
 */
export function replaceSemanticFavicon(ownerDocument: Document, href: string, state: FleetAggregateState): void {
  const replacement = ownerDocument.createElement("link");
  replacement.id = semanticFaviconId;
  replacement.rel = "icon";
  replacement.type = "image/x-icon";
  replacement.href = href;
  replacement.dataset.aggregateState = state;
  replacement.setAttribute("sizes", "16x16 20x20 24x24 32x32");

  const current = ownerDocument.getElementById(semanticFaviconId)
    ?? ownerDocument.querySelector<HTMLLinkElement>('link[rel~="icon"]');
  if (current) {
    current.replaceWith(replacement);
  } else {
    ownerDocument.head.append(replacement);
  }

  // Remove older or third-party favicon candidates so the semantic replacement remains the single active source.
  for (const candidate of ownerDocument.querySelectorAll<HTMLLinkElement>('link[rel~="icon"]')) {
    if (candidate !== replacement) candidate.remove();
  }
}
